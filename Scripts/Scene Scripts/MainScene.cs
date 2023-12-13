using Godot;
using System;

namespace ADK
{
    public partial class MainScene : Node
    {
        [Export] Button offlineButton, onlineButton;

        public override void _Ready()
        {
            base._Ready();
            offlineButton.Pressed += OnOfflineButtonClicked;
            onlineButton.Pressed += OnOnlineButtonClicked;
        }

        private void OnOfflineButtonClicked()
        {
            GameManager.Instance.GoToScene(GameScene.Lobby);
        }

        void OnOnlineButtonClicked()
        {
            GameManager.Instance.GoToScene(GameScene.NetLobby);
        }
    }
}
