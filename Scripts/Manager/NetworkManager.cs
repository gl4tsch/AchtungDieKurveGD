using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ADK.Net
{
    public partial class NetworkManager : Node
    {
        public static NetworkManager Instance { get; private set; }

        [Export] int port = 1414;
        public int Port => port;
        [Export] string defaultServerIP = "localhost";
        [Export] int maxConnections = 99;
        /// <summary>
        /// This sets Physics Ticks per Second
        /// </summary>
        [Export] public int TicksPerSecond
        {
            get => Engine.PhysicsTicksPerSecond;
            set => Engine.PhysicsTicksPerSecond = value;
        }

        public event Action<(long id, PlayerInfo info)> PlayerConnected;
        public event Action<(long id, PlayerInfo info)> PlayerInfoChanged;
        public event Action<long> PlayerDisconnected;
        public event Action ServerDisconnected;
        public event Action AllReadyOneshot;

        PlayerInfo localPlayerInfo;
        public PlayerInfo LocalPlayerInfo
        {
            get => localPlayerInfo;
            set
            {
                localPlayerInfo = value;
                SendPlayerInfoUpdate();
            }
        }
        Dictionary<long, PlayerInfo> players = new();
        public IReadOnlyDictionary<long, PlayerInfo> Players => players;
        List<long> readyPlayers = new();
        bool isEveryoneReady => players.Keys.All(p => readyPlayers.Contains(p));

        public NetworkManager()
        {
            Instance = this;
            localPlayerInfo = new();
            var rng = new RandomNumberGenerator();
            localPlayerInfo.Color = Color.FromHsv(rng.Randf(), 1, 1);
        }

        public override void _Ready()
        {
            Engine.PhysicsTicksPerSecond = TicksPerSecond;
            Multiplayer.PeerConnected += OnPeerConnected;
            Multiplayer.PeerDisconnected += OnPeerDisconnected;
            Multiplayer.ConnectedToServer += OnConnectedToServer;
            Multiplayer.ConnectionFailed += OnConnectionFailed;
            Multiplayer.ServerDisconnected += OnServerDisconnected;
        }

        public void SetPort(int port)
        {
            this.port = port;
        }

        public bool HostGameUpnp()
        {
            Upnp upnp = new();
            Upnp.UpnpResult discoverResult = (Upnp.UpnpResult)upnp.Discover();

            GD.Print("UPNP discover: " + discoverResult);
            if (discoverResult != Upnp.UpnpResult.Success)
            {
                return false;
            }

            GD.Print("UPNP valid gateway? " + upnp.GetGateway().IsValidGateway());
            if (!(upnp.GetGateway()?.IsValidGateway() ?? false))
            {
                return false;
            }

            GD.Print("UPNP valid gateway at " + upnp.QueryExternalAddress());
            Upnp.UpnpResult portMapResult = (Upnp.UpnpResult)upnp.AddPortMapping(port, 0, "AchtungDieKurve_UPD", "UDP");
            GD.Print("UPNP UDP port mapping result: " + portMapResult);
            portMapResult = (Upnp.UpnpResult)upnp.AddPortMapping(port, 0, "AchtungDieKurve_TCP", "TCP");
            GD.Print("UPNP TCP port mapping result: " + portMapResult);
            
            return HostGame();
        }

        public bool HostGame()
        {
            ENetMultiplayerPeer peer = new();

            var error = peer.CreateServer(port, maxConnections);
            if (error != Error.Ok)
            {
                GD.PrintErr($"Error trying to host: {error}");
                return false;
            }
            GD.Print("Server ready");

            // connect to own game
            Multiplayer.MultiplayerPeer = peer;
            UpdatePlayerInfo(1, localPlayerInfo.Name, localPlayerInfo.Color, localPlayerInfo.Ability);
            return true;
        }

        public bool JoinGame(string hostIP = null)
        {
            if (hostIP == null)
            {
                hostIP = defaultServerIP;
            }
            ENetMultiplayerPeer peer = new();
            var error = peer.CreateClient(hostIP, port);
            if (error != Error.Ok)
            {
                GD.PrintErr($"Error trying to join host at {hostIP}: {error}");
                return false;
            }
            Multiplayer.MultiplayerPeer = peer;
            GD.Print("Joining...");
            return true;
        }

        public void Disconnect()
        {
            OnServerDisconnected();
        }

        /// <summary>
        /// gets called on all peers
        /// </summary>
        /// <param name="id">connected player</param>
        private void OnPeerConnected(long id)
        {
            GD.Print($"Player Connected: {id}. Handled by server");
        }

        /// <summary>
        /// gets called on all peers
        /// </summary>
        /// <param name="id">disconnected player</param>
        private void OnPeerDisconnected(long id)
        {
            GD.Print($"Player Disconnected: {id}.");

            players.Remove(id);
            PlayerDisconnected?.Invoke(id);
        }

        /// <summary>
        /// gets called on this client
        /// </summary>
        private void OnConnectedToServer()
        {
            GD.Print("Connected to Server");
            SendPlayerInfoUpdate();
        }

        /// <summary>
        /// gets called on this client
        /// </summary>
        private void OnConnectionFailed()
        {
            GD.Print("Connection Failed");
            OnServerDisconnected();
        }

        /// <summary>
        /// gets called on all clients
        /// </summary>
        private void OnServerDisconnected()
        {
            Multiplayer.MultiplayerPeer?.Close();
            Multiplayer.MultiplayerPeer = null;
            players.Clear();
            ServerDisconnected?.Invoke();
        }

        void SendPlayerInfoUpdate()
        {
            // send my info to the server
            var myId = Multiplayer.GetUniqueId();
            if (Multiplayer.IsServer())
            {
                UpdatePlayerInfo(myId, LocalPlayerInfo.Name, localPlayerInfo.Color, localPlayerInfo.Ability);
            }
            else
            {
                RpcId(1, nameof(UpdatePlayerInfo), myId, LocalPlayerInfo.Name, LocalPlayerInfo.Color, LocalPlayerInfo.Ability);
            }
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        void UpdatePlayerInfo(long id, string name, Color color, int ability)
        {
            PlayerInfo info = new()
            {
                Name = name,
                Color = color,
                Ability = ability
            };

            if (players.ContainsKey(id))
            {
                players[id] = info;
                PlayerInfoChanged?.Invoke((id, info));
            }
            else
            {
                players.Add(id, info);
                PlayerConnected?.Invoke((id, info));
            }

            // if i am the server, send info to all clients
            if (Multiplayer.IsServer())
            {
                foreach (var player in players)
                {
                    Rpc(nameof(UpdatePlayerInfo), player.Key, player.Value.Name, player.Value.Color, player.Value.Ability);
                }
            }
        }

        // only the server may start the game
        public void SendStartGame()
        {
            if (Multiplayer.IsServer())
            {
                Rpc(nameof(StartGame));
            }
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        void StartGame()
        {
            GameManager.Instance.GoToScene(GameScene.NetArena);
        }

        // tell the server i am ready
        public void SendReady()
        {
            RpcId(1, nameof(SetReady));
        }

        // server
        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        void SetReady()
        {
            if (!Multiplayer.IsServer())
            {
                return;
            }

            long playerId = Multiplayer.GetRemoteSenderId();
            if (!readyPlayers.Contains(playerId))
            {
                readyPlayers.Add(playerId);
            }
            if (isEveryoneReady)
            {
                // clear the list for the next ready check
                readyPlayers.Clear();
                Rpc(nameof(ReadyToRumble));
            }
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        void ReadyToRumble()
        {
            var oneshot = AllReadyOneshot;
            AllReadyOneshot = null;
            oneshot?.Invoke();
        }
    }
}
