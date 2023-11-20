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

        public override void _Ready()
        {
            base._Ready();
            startButton.Pressed += OnStartButtonClicked;
            AudioManager.Instance?.PlayMusic(Music.LobbyTheme);
        }

        void OnStartButtonClicked()
        {
            // change to arena scene
            GameManager.Instance.GoToScene(GameScene.Arena);
        }
    }
}
