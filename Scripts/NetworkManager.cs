using Godot;
using System;
using System.Collections.Generic;

namespace ADK.Net
{
    public partial class NetworkManager : Node
    {
        public static NetworkManager Instance { get; private set; }

        [Export] int port = 1414;
        [Export] string defaultServerIP = "localhost";
        [Export] int maxConnections = 99;

        public event Action<(int id, PlayerInfo info)> PlayerConnected;
        public event Action<int> PlayerDisconnected;
        public event Action ServerDisconnected;

        PlayerInfo localPlayerInfo;
        Dictionary<long, PlayerInfo> players;

        public NetworkManager()
        {
            Instance = this;
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
            GD.Print("Join Success");
        }

        /// <summary>
        /// runs on all peers.
        /// when a peer connects, send them my player info.
        /// </summary>
        /// <param name="id">connected player</param>
        private void OnPeerConnected(long id)
        {
            GD.Print($"Player Connected: {id}");
            Rpc(nameof(RegisterPlayer), localPlayerInfo.Name, localPlayerInfo.Color);
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        void RegisterPlayer(string name, Color color)
        {
            int id = Multiplayer.GetRemoteSenderId();
            PlayerInfo newPlayer = new()
            {
                Name = name,
                Color = color
            };
            players[id] = newPlayer;
            PlayerConnected?.Invoke((id, newPlayer));
        }

        /// <summary>
        /// runs on all peers
        /// </summary>
        /// <param name="id">disconnected player</param>
        private void OnPeerDisconnected(long id)
        {
            GD.Print($"Player Disconnected: {id}");
        }

        /// <summary>
        /// runs on clients only
        /// </summary>
        private void OnConnectedToServer()
        {
            GD.Print("Connected to Server");
        }

        /// <summary>
        /// runs on clients only
        /// </summary>
        private void OnConnectionFailed()
        {
            GD.Print("Connection Failed");
        }

        /// <summary>
        /// runs on clinets only
        /// </summary>
        private void OnServerDisconnected()
        {
        }
    }
}
