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

        [Export] PackedScene mainScene;
        [Export] PackedScene lobbyScene;
        [Export] PackedScene netLobbyScene;
        [Export] PackedScene arenaScene;

        public Settings Settings { get; private set; }
        /// <summary>
        /// this is used as a centralized mapping from index to ability
        /// </summary>
        public List<(string name, Func<Ability> creator)> AbilityFactory;
        public List<Snake> Snakes = new();

        public GameManager()
        {
            Instance = this;

            // load settings
            Settings = new();
            Settings.LoadSettings();
            ApplySettings(Settings);

            // create ability factory
            AbilityFactory = new()
            {
                (Ability.NoAbilityDisplayName, () => null),
                (EraserAbility.DisplayName, () => new EraserAbility(Settings.AbilitySettings)),
                (SpeedAbility.DisplayName, () => new SpeedAbility(Settings.AbilitySettings)),
                (TeleportAbility.DisplayName, () => new TeleportAbility(Settings.AbilitySettings)),
                (TBarAbility.DisplayName, () => new TBarAbility(Settings.AbilitySettings)),
                (VBarAbility.DisplayName, () => new VBarAbility(Settings.AbilitySettings))
            };

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
            ApplyGraphicsSettings();
            ApplyAudioSettings();
            ApplyArenaSettings();
            ApplySnakeSettings();
            ApplyAbilitySettings();
        }

        public void ApplyGraphicsSettings()
        {
            DisplayServer.WindowSetVsyncMode(Settings.GraphicsSettings.VSyncSetting);
            Engine.MaxFps = Settings.GraphicsSettings.FPSLimitSetting;
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

        public Ability CreateAbility(int idx)
        {
            return AbilityFactory[idx].creator();
        }

        public int GetAbilityIndex(Ability ability)
        {
            return ability == null ? 0 : AbilityFactory.FindIndex(a => a.name == ability.Name);
        }

        public string GetAbilityName(int idx)
        {
            return AbilityFactory[idx].name;
        }

        public void GoToScene(GameScene scene)
        {
            switch (scene)
            {
                case GameScene.Main:
                    CallDeferred(MethodName.DeferredGoToScene, mainScene);
                    break;
                case GameScene.Lobby:
                    CallDeferred(MethodName.DeferredGoToScene, lobbyScene);
                    break;
                case GameScene.NetLobby:
                    CallDeferred(MethodName.DeferredGoToScene, netLobbyScene);
                    break;
                case GameScene.Arena:
                    CallDeferred(MethodName.DeferredGoToScene, arenaScene);
                    break;
            }
        }

        void DeferredGoToScene(PackedScene scene)
        {
            CurrentScene?.Free();
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
        Main,
        Lobby,
        NetLobby,
        Arena
    }
}
