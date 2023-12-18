using ADK.Net;
using ADK.UI;
using Godot;

namespace ADK
{
    public partial class NetLobbyScene : Node
    {
        [Export] Button backButton;
        [Export] LobbySnake ownSnake;
        [Export] Button hostButton, joinButton, startButton, readyButton;
        [Export] NetworkLobby lobby;
        [Export] Control lobbyContent;

        enum NetLobbyState
        {
            Disconnected,
            Host,
            Client
        }
        NetLobbyState lobbyState = NetLobbyState.Disconnected;

        PlayerInfo ownPlayerInfo => NetworkManager.Instance.LocalPlayerInfo;

        float timer = 0;
        float updateInterval = 1f; // in seconds

        public override void _Ready()
        {
            base._Ready();

            Ability playerInfoAbility = GameManager.Instance.CreateAbility(ownPlayerInfo.Ability);
            ownSnake.Init(new Snake(ownPlayerInfo.Name, ownPlayerInfo.Color, playerInfoAbility));

            hostButton.Pressed += OnHostButtonClicked;
            joinButton.Pressed += OnJoinButtonClicked;
            readyButton.Pressed += OnReadyButtonClicked;
            startButton.Pressed += OnStartButtonClicked;
            backButton.Pressed += GoBack;

            SetLobbyState(NetLobbyState.Disconnected);
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

        public override void _Process(double delta)
        {
            base._Process(delta);

            // check regularly for changes to own snake
            timer += (float)delta;
            if (timer > updateInterval)
            {
                timer -= updateInterval;
                // update network manager info for own snake if needed
                int abilityIdx = GameManager.Instance.GetAbilityIndex(ownSnake.Snake.Ability);
                if (ownSnake.Snake.Name != ownPlayerInfo.Name ||
                    ownSnake.Snake.Color != ownPlayerInfo.Color ||
                    abilityIdx != ownPlayerInfo.Ability)
                {
                    NetworkManager.Instance.LocalPlayerInfo = new(ownSnake.Snake.Name, ownSnake.Snake.Color, abilityIdx);
                }
            }
        }

        void SetLobbyState(NetLobbyState state)
        {
            lobbyState = state;

            // buttons
            hostButton.Visible = state == NetLobbyState.Disconnected;
            joinButton.Visible = state == NetLobbyState.Disconnected;
            startButton.Visible = state == NetLobbyState.Host;
            readyButton.Visible = state != NetLobbyState.Disconnected;

            // lobby
            lobbyContent.Visible = state != NetLobbyState.Disconnected;
        }

        void OnHostButtonClicked()
        {
            if (NetworkManager.Instance.HostGame())
            {
                SetLobbyState(NetLobbyState.Host);
            }
        }

        void OnJoinButtonClicked()
        {
            if (NetworkManager.Instance.JoinGame())
            {
                SetLobbyState(NetLobbyState.Client);
            }
        }

        void OnReadyButtonClicked()
        {

        }

        void OnStartButtonClicked()
        {
            NetworkManager.Instance.SendStartGame();
        }

        void GoBack()
        {
            NetworkManager.Instance.Disconnect();
            GameManager.Instance.GoToScene(GameScene.Main);
        }
    }
}
