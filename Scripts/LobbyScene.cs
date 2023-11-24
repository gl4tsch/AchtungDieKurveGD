using ADK.UI;
using Godot;
using System;

namespace ADK
{
    public partial class LobbyScene : Control
    {
        [Export] Button startButton;
        [Export] Button settingsButton;
        [Export] SnakeLobby snakeLobby;
        [Export] PackedScene settingsWindowPrefab;

        public override void _Ready()
        {
            base._Ready();

            startButton.Pressed += OnStartButtonClicked;
            settingsButton.Pressed += OnSettingsButtonClicked;

            AudioManager.Instance?.PlayMusic(Music.LobbyTheme);
        }

        void OnStartButtonClicked()
        {
            // change to arena scene
            GameManager.Instance.GoToScene(GameScene.Arena);
        }

        void OnSettingsButtonClicked()
        {
            var settingsWindow = settingsWindowPrefab.Instantiate<SettingsWindow>();
            AddChild(settingsWindow);
        }
    }
}
