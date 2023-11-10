using Godot;
using System;
using System.Collections.Generic;

namespace ADK
{
    public partial class GameManager : Node
    {
        public static GameManager Instance { get; private set; }

        public Node CurrentScene { get; private set; }

        string lobbyScenePath = "res://LobbyScene.tscn";
        string arenaScenePath = "res://ArenaScene.tscn";

        public List<Snake> Snakes = new();

        public GameManager()
        {
            Instance = this;
            // init default snakes
            Snakes.Add(new Snake());
            Snakes.Add(new Snake());
            Snakes.Add(new Snake());
        }

        public override void _Ready()
        {
            base._Ready();

            // // avoid for now because of
            // // "Autoloads must not be removed using free() or queue_free() at runtime,
            // // or the engine will crash."
            // if (Instance != null && Instance != this)
            // {
            //     Free();
            //     return;
            // }
            // Instance = this;

            Viewport root = GetTree().Root;
            CurrentScene = root.GetChild(root.GetChildCount() - 1);
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
