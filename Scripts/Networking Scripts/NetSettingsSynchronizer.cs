using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ADK.Net
{
    public partial class NetSettingsSynchronizer : Node
    {
        public Settings NetSettings => settingsCopy;
        Settings settingsCopy;
        SettingsSection netArenaSettings => settingsCopy.ArenaSettings;
        SettingsSection netSnakeSettings => settingsCopy.SnakeSettings;
        SettingsSection netAbilitySettings => settingsCopy.AbilitySettings;

        // server only
        Dictionary<long, int> pendingConfirmations = new();
        public bool ConfirmationsPending => pendingConfirmations.Values.Any(c => c > 0);

        public override void _Ready()
        {
            NetworkManager.Instance.PlayerConnected += OnPlayerConnected;
            NetworkManager.Instance.PlayerDisconnected += OnPlayerDisconnected;
            NetworkManager.Instance.ServerDisconnected += Clear;
            Clear();
        }

        public override void _ExitTree()
        {
            NetworkManager.Instance.PlayerConnected -= OnPlayerConnected;
            NetworkManager.Instance.PlayerDisconnected -= OnPlayerDisconnected;
            NetworkManager.Instance.ServerDisconnected -= Clear;
        }

        void Clear()
        {
            settingsCopy = GameManager.Instance.Settings.NewCopy();
            pendingConfirmations = new();
        }

        void OnPlayerConnected((long id, PlayerInfo info) player)
        {
            if (!Multiplayer.IsServer()) return;

            SendSettingsToPlayer(player.id);
        }

        void OnPlayerDisconnected(long playerId)
        {
            pendingConfirmations.Remove(playerId);
        }

        // server
        void SendSettingsToAll()
        {
            if (!Multiplayer.IsServer()) return;

            foreach (long player in NetworkManager.Instance.Players.Keys)
            {
                SendSettingsToPlayer(player);
            }
        }

        // server
        void SendSettingsToPlayer(long playerId)
        {
            if (!Multiplayer.IsServer()) return;

            GD.Print("Sending settings to player " + playerId);
            SendArenaSettings(playerId);
            SendSnakeSettings(playerId);
            SendAbilitySettings(playerId);
        }

        void ModifyPending(long playerId, int delta)
        {
            if (!pendingConfirmations.ContainsKey(playerId))
            {
                pendingConfirmations.Add(playerId, 0);
            }
            pendingConfirmations[playerId] += delta;
        }

        // server
        void SendArenaSettings(long playerId)
        {
            if (!Multiplayer.IsServer()) return;

            ModifyPending(playerId, 1);
            RpcId(playerId, nameof(ReceiveArenaSettings), netArenaSettings.ToGodotDict());
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        void ReceiveArenaSettings(Godot.Collections.Dictionary<string, Variant> settings)
        {
            netArenaSettings.LoadFromGodotDict(settings);
            GD.Print($"{Multiplayer.GetUniqueId()} received arena settings from server: {settings}");
            RpcId(1, nameof(ReceiveSettingsConfirmation));
        }

        // server
        void SendSnakeSettings(long playerId)
        {
            if (!Multiplayer.IsServer()) return;

            ModifyPending(playerId, 1);
            RpcId(playerId, nameof(ReceiveSnakeSettings), netSnakeSettings.ToGodotDict());
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        void ReceiveSnakeSettings(Godot.Collections.Dictionary<string, Variant> settings)
        {
            netSnakeSettings.LoadFromGodotDict(settings);
            GD.Print($"{Multiplayer.GetUniqueId()} received snake settings from server: {settings}");
            RpcId(1, nameof(ReceiveSettingsConfirmation));
        }

        // server
        void SendAbilitySettings(long playerId)
        {
            if (!Multiplayer.IsServer()) return;

            ModifyPending(playerId, 1);
            RpcId(playerId, nameof(ReceiveAbilitySettings), netAbilitySettings.ToGodotDict());
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        void ReceiveAbilitySettings(Godot.Collections.Dictionary<string, Variant> settings)
        {
            netAbilitySettings.LoadFromGodotDict(settings);
            GD.Print($"{Multiplayer.GetUniqueId()} received ability settings from server: {settings}");
            RpcId(1, nameof(ReceiveSettingsConfirmation));
        }

        // server
        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        void ReceiveSettingsConfirmation()
        {
            long sender = Multiplayer.GetRemoteSenderId();
            ModifyPending(sender, -1);
        }
    }
}
