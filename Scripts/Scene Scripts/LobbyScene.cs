using ADK.UI;
using Godot;
using System;

namespace ADK
{
    public partial class LobbyScene : Control
    {
        [Export] Button startButton;
        [Export] Button settingsButton;
        [Export] Button backButton;
        [Export] SnakeLobby snakeLobby;
        [Export] PackedScene settingsWindowPrefab;

        public override void _Ready()
        {
            base._Ready();

            startButton.Pressed += OnStartButtonClicked;
            settingsButton.Pressed += OnSettingsButtonClicked;
            backButton.Pressed += GoBack;

            AudioManager.Instance?.PlayMusic(Music.LobbyTheme);
        }

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);
            if (@event is InputEventKey keyEvent && keyEvent.IsPressed() && !keyEvent.IsEcho())
            {
                if (keyEvent.Keycode == Key.Escape)
                {
                    GoBack();
                }
            }
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

        void GoBack()
        {
            GameManager.Instance.GoToScene(GameScene.Main);
        }
    }
}
