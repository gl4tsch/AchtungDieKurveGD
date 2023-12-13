using ADK.Net;
using ADK.UI;
using Godot;
using System;

namespace ADK
{
    public partial class NetLobbyScene : Node
    {
        [Export] Button hostButton, joinButton;
        [Export] NetworkLobby lobby;
        [Export] Control preLobbyContent, lobbyContent;

        public override void _Ready()
        {
            base._Ready();

            hostButton.Pressed += OnHostButtonClicked;
            joinButton.Pressed += OnJoinButtonClicked;
        }

        void OnHostButtonClicked()
        {
            NetworkManager.Instance.HostGame();
            preLobbyContent.Visible = false;
            lobbyContent.Visible = true;   
        }

        void OnJoinButtonClicked()
        {
            NetworkManager.Instance.JoinGame();
            preLobbyContent.Visible = false;
            lobbyContent.Visible = true;
        }
    }
}
