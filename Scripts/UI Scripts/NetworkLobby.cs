using ADK.Net;
using Godot;
using System.Collections.Generic;

namespace ADK.UI
{
    public partial class NetworkLobby : Control
    {
        [Export] Control netSnakeContainer;
        [Export] PackedScene netSnakePrefab;

        Dictionary<long, NetworkLobbySnake> netSnakeInstances = new();

        public override void _Ready()
        {
            base._Ready();
            
            ClearNetSnakeInstances();

            NetworkManager.Instance.PlayerConnected += OnPlayerConnected;
            NetworkManager.Instance.PlayerInfoChanged += OnPlayerInfoChanged;
            NetworkManager.Instance.PlayerDisconnected += OnPlayerDisconnected;
            NetworkManager.Instance.ServerDisconnected += OnServerDisconnected;
            NetworkManager.Instance.RttChecker.RttUpdateForPlayer += OnRttUpdateForPlayer;
        }

        public override void _ExitTree()
        {
            base._ExitTree();

            NetworkManager.Instance.PlayerConnected -= OnPlayerConnected;
            NetworkManager.Instance.PlayerInfoChanged -= OnPlayerInfoChanged;
            NetworkManager.Instance.PlayerDisconnected -= OnPlayerDisconnected;
            NetworkManager.Instance.ServerDisconnected -= OnServerDisconnected;
            NetworkManager.Instance.RttChecker.RttUpdateForPlayer -= OnRttUpdateForPlayer;
        }

        void ClearNetSnakeInstances()
        {
            foreach (var child in netSnakeContainer.GetChildren())
            {
                child.QueueFree();
            }
            netSnakeInstances.Clear();
        }

        void OnPlayerConnected((long id, PlayerInfo info) player)
        {
            var netSnake = netSnakePrefab.Instantiate<NetworkLobbySnake>().Init(player.info);
            netSnakeContainer.AddChild(netSnake);
            netSnakeInstances.Add(player.id, netSnake);
        }

        void OnPlayerInfoChanged((long id, PlayerInfo info) player)
        {
            if (netSnakeInstances.ContainsKey(player.id))
            {
                netSnakeInstances[player.id].Init(player.info);
            }
        }

        void OnPlayerDisconnected(long id)
        {
            if (netSnakeInstances.ContainsKey(id))
            {
                netSnakeInstances[id].QueueFree();
                netSnakeInstances.Remove(id);
            }
        }

        void OnServerDisconnected()
        {
            foreach (var netSnake in netSnakeInstances)
            {
                netSnake.Value.QueueFree();
            }
            netSnakeInstances.Clear();
        }

        void OnRttUpdateForPlayer((long playerId, float rttMs)rttUpdate)
        {
            if (netSnakeInstances.ContainsKey(rttUpdate.playerId))
            {
                netSnakeInstances[rttUpdate.playerId].UpdatePing(rttUpdate.rttMs / 2f);
            }
        }
    }
}
