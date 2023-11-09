using Godot;
using System;
using System.Collections.Generic;

namespace ADK.UI
{
    public partial class SnakeLobby : Control
    {
        [Export] PackedScene lobbySnakePrefab;
        [Export] BoxContainer snakeContainer;
        [Export] Button newSnakeButton;

        List<LobbySnake> lobbySnakes = new();

        public override void _Ready()
        {
            base._Ready();

            newSnakeButton.Pressed += AddNewSnake;

            ClearLobby();
            AddNewSnake();
            AddNewSnake();
            AddNewSnake();
        }

        void ClearLobby()
        {
            foreach (var child in snakeContainer.GetChildren())
            {
                child.QueueFree();
            }
            lobbySnakes.Clear();
        }

        public void AddNewSnake()
        {
            LobbySnake lobbySnake = lobbySnakePrefab.Instantiate<LobbySnake>();
            lobbySnake.Lobby = this;
            lobbySnakes.Add(lobbySnake);
            snakeContainer.AddChild(lobbySnake);
        }

        public void DeleteSnake(LobbySnake snake)
        {
            snake.QueueFree();
            lobbySnakes.Remove(snake);
        }
    }
}
