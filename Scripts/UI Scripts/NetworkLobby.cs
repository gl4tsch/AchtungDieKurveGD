using ADK.Net;
using Godot;
using System;
using System.Collections.Generic;

namespace ADK.UI
{
    public partial class NetworkLobby : Control
    {
        [Export] LineEdit playerNameInput;
        [Export] HueSlider colorPicker;
        [Export] Button hostButton, joinButton;
        [Export] Control netSnakeContainer;
        [Export] PackedScene netSnakePrefab;

        Dictionary<long, NetworkLobbySnake> netSnakeInstances = new();

        public override void _Ready()
        {
            base._Ready();
            NetworkManager.Instance.PlayerConnected += OnPlayerConnected;
            NetworkManager.Instance.PlayerDisconnected += OnPlayerDisconnected;
            NetworkManager.Instance.ServerDisconnected += OnServerDisconnected;
        }

        void OnPlayerConnected((long id, PlayerInfo info) player)
        {
            var netSnake = netSnakePrefab.Instantiate<NetworkLobbySnake>().Init(player.info);
            netSnakeContainer.AddChild(netSnake);
            netSnakeInstances.Add(player.id, netSnake);
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
    }
}
