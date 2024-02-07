using ADK.Net;
using ADK.UI;
using Godot;

namespace ADK
{
    public partial class NetLobbyScene : Node
    {
        [Export] NetSettingsSynchronizer netSettingsSynchronizer;
        [Export] LineEdit ipInput, portInput;
        [Export] Button backButton;
        [Export] LobbySnake ownSnake;
        [Export] Button hostButton, joinButton, startButton, readyButton, leaveButton;
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
            ipInput.TextChanged += OnIpInput;
            portInput.Text = NetworkManager.Instance.Port.ToString();
            portInput.TextChanged += OnPortInput;

            GameManager.Instance.Snakes.Clear();
            var snake = GameManager.Instance.CreateNewSnake();

            Ability playerInfoAbility = GameManager.Instance.CreateAbility(ownPlayerInfo.Ability);
            snake.Ability = playerInfoAbility;
            snake.Color = ownPlayerInfo.Color;
            snake.Name = ownPlayerInfo.Name;
            ownSnake.Init(snake);

            hostButton.Pressed += OnHostButtonClicked;
            joinButton.Pressed += OnJoinButtonClicked;
            readyButton.Pressed += OnReadyButtonClicked;
            startButton.Pressed += OnStartButtonClicked;
            leaveButton.Pressed += OnLeaveButtonClicked;
            backButton.Pressed += GoBack;

            SetLobbyState(NetLobbyState.Disconnected);
            NetworkManager.Instance.ServerDisconnected += () => SetLobbyState(NetLobbyState.Disconnected);
        }

        public override void _ExitTree()
        {
            NetworkManager.Instance.RttChecker.DoRegularRttChecks = false;
        }

        void OnIpInput(string ip)
        {
            // todo: check for validity
        }

        void OnPortInput(string port)
        {
            if (int.TryParse(port, out int portNum))
            {
                NetworkManager.Instance.SetPort(portNum);
            }
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
            readyButton.Visible = false; //state != NetLobbyState.Disconnected;
            leaveButton.Visible = state != NetLobbyState.Disconnected;

            // lobby
            lobbyContent.Visible = state != NetLobbyState.Disconnected;
            NetworkManager.Instance.RttChecker.DoRegularRttChecks = state == NetLobbyState.Host;
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
            if (NetworkManager.Instance.JoinGame(ipInput.Text))
            {
                SetLobbyState(NetLobbyState.Client);
            }
            else
            {
                SetLobbyState(NetLobbyState.Disconnected);
            }
        }

        void OnReadyButtonClicked()
        {

        }

        void OnStartButtonClicked()
        {
            if (netSettingsSynchronizer.ConfirmationsPending)
            {
                GD.PrintErr("Need to wait for all clients to receive and confirm settings");
                return;
            }
            GameManager.Instance.ApplySettings(netSettingsSynchronizer.NetSettings);
            NetworkManager.Instance.SendStartGame();
        }

        void OnLeaveButtonClicked()
        {
            NetworkManager.Instance.Disconnect();
            SetLobbyState(NetLobbyState.Disconnected);
        }

        void GoBack()
        {
            NetworkManager.Instance.Disconnect();
            GameManager.Instance.GoToScene(GameScene.Main);
        }
    }
}
