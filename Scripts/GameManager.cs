using System.Reflection;
using Godot;
using System;
using System.Collections.Generic;

namespace ADK
{
    public partial class GameManager : Node
    {
        public static GameManager Instance { get; private set; }

        public Node CurrentScene { get; private set; }
        public LobbyScene ActiveLobbyScene => CurrentScene as LobbyScene;
        public ArenaScene ActiveArenaScene => CurrentScene as ArenaScene;

        string lobbyScenePath = "res://LobbyScene.tscn";
        string arenaScenePath = "res://ArenaScene.tscn";

        public Settings Settings { get; private set; }
        public List<Snake> Snakes = new();

        public GameManager()
        {
            Instance = this;
            // load settings
            Settings = new();
            Settings.LoadSettings();
            ApplySettings(Settings);
            // init default snakes
            CreateNewSnake();
            CreateNewSnake();
            CreateNewSnake();
        }

        public override void _Ready()
        {
            base._Ready();

            Viewport root = GetTree().Root;
            CurrentScene = root.GetChild(root.GetChildCount() - 1);
        }

        public Snake CreateNewSnake()
        {
            var snakeSettings = Settings.SnakeSettings;
            var snake = new Snake($"Snake {Snakes.Count + 1}", snakeSettings);
            Snakes.Add(snake);
            return snake;
        }

        public bool RemoveSnake(Snake snake)
        {
            return Snakes.Remove(snake);
        }

        public void ApplySettings(Settings settings)
        {
            Settings = settings;
            ApplyAudioSettings();
            ApplyArenaSettings();
            ApplySnakeSettings();
            ApplyAbilitySettings();
        }

        public void ApplyAudioSettings()
        {
            AudioManager.Instance?.ApplySettings(Settings.AudioSettings);
        }

        public void ApplyArenaSettings()
        {
            // nothing to do, as arena settings are read directly by the Arena every time
        }

        public void ApplySnakeSettings()
        {
            foreach (var snake in Snakes)
            {
                snake.ApplySettings(Settings.SnakeSettings);
            }
        }

        public void ApplyAbilitySettings()
        {
            foreach (var snake in Snakes)
            {
                snake.Ability?.ApplySettings(Settings.AbilitySettings);
            }
        }

        public void GoToScene(GameScene scene)
        {
            switch (scene)
            {
                case GameScene.Lobby:
                    CallDeferred(MethodName.DeferredGoToScene, lobbyScenePath);
                    break;
                case GameScene.Arena:
                    CallDeferred(MethodName.DeferredGoToScene, arenaScenePath);
                    break;
            }
        }

        void DeferredGoToScene(string path)
        {
            CurrentScene?.Free();
            var scene = GD.Load<PackedScene>(path);
            CurrentScene = scene?.Instantiate();

            if (CurrentScene != null)
            {
                GetTree().Root.AddChild(CurrentScene);
                // To make it compatible with the SceneTree.change_scene_to_file() API.
                GetTree().CurrentScene = CurrentScene;
            }
        }
    }

    public enum GameScene
    {
        Lobby,
        Arena
    }
}
