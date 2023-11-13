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
            newSnakeButton.Pressed += CreateNewSnake;
            Init(GameManager.Instance.Snakes);
        }

        public void Init(List<Snake> snakes)
        {
            ClearLobby();
            foreach (var snake in snakes)
            {
                AddSnakeToLobby(snake);
            }
        }

        void ClearLobby()
        {
            foreach (var child in snakeContainer.GetChildren())
            {
                child.QueueFree();
            }
            lobbySnakes.Clear();
        }

        public void CreateNewSnake()
        {
            Snake snake = GameManager.Instance.CreateNewSnake();
            AddSnakeToLobby(snake);
        }

        public void AddSnakeToLobby(Snake snake)
        {
            LobbySnake lobbySnake = lobbySnakePrefab.Instantiate<LobbySnake>().Init(snake);
            lobbySnake.Lobby = this;
            lobbySnakes.Add(lobbySnake);
            snakeContainer.AddChild(lobbySnake);
        }

        public void DeleteSnake(LobbySnake snake)
        {
            snake.QueueFree();
            lobbySnakes.Remove(snake);
            GameManager.Instance.RemoveSnake(snake.Snake);
        }
    }
}
