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
        [Export] string defaultServerIP = "localhost";
        [Export] int maxConnections = 99;

        public long OwnId = -1;

        public event Action<(long id, PlayerInfo info)> PlayerConnected;
        public event Action<(long id, PlayerInfo info)> PlayerInfoChanged;
        public event Action<long> PlayerDisconnected;
        public event Action ServerDisconnected;

        PlayerInfo localPlayerInfo;
        Dictionary<long, PlayerInfo> players = new();

        public NetworkManager()
        {
            Instance = this;
            localPlayerInfo = new();
        }

        public override void _Ready()
        {
            base._Ready();
            Multiplayer.PeerConnected += OnPeerConnected;
            Multiplayer.PeerDisconnected += OnPeerDisconnected;
            Multiplayer.ConnectedToServer += OnConnectedToServer;
            Multiplayer.ConnectionFailed += OnConnectionFailed;
            Multiplayer.ServerDisconnected += OnServerDisconnected;
        }

        public void HostGame()
        {
            ENetMultiplayerPeer peer = new();
            var error = peer.CreateServer(port, maxConnections);
            if (error != Error.Ok)
            {
                GD.Print($"Error trying to host: {error}");
                return;
            }
            GD.Print("Server ready");

            // connect to own game
            Multiplayer.MultiplayerPeer = peer;
            players.Add(1, localPlayerInfo);
            PlayerConnected?.Invoke((1, localPlayerInfo));
        }

        public void JoinGame(string hostIP = null)
        {
            if (hostIP == null)
            {
                hostIP = defaultServerIP;
            }
            ENetMultiplayerPeer peer = new();
            var error = peer.CreateClient(hostIP, port);
            if (error != Error.Ok)
            {
                GD.Print($"Error trying to join host at {hostIP}: {error}");
                return;
            }
            Multiplayer.MultiplayerPeer = peer;
            GD.Print("Joining...");
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

            var myId = Multiplayer.GetUniqueId();
            // send my info to the server
            RpcId(1, nameof(UpdatePlayerInfo), myId, localPlayerInfo.Name, localPlayerInfo.Color, localPlayerInfo.Ability);
        }

        /// <summary>
        /// gets called on this client
        /// </summary>
        private void OnConnectionFailed()
        {
            GD.Print("Connection Failed");
            Multiplayer.MultiplayerPeer = null;
        }

        /// <summary>
        /// gets called on all clients
        /// </summary>
        private void OnServerDisconnected()
        {
            Multiplayer.MultiplayerPeer = null;
            players.Clear();
            ServerDisconnected?.Invoke();
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
    }
}
