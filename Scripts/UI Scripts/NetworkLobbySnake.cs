using ADK.Net;
using Godot;
using System;

namespace ADK.UI
{
    public partial class NetworkLobbySnake : HBoxContainer
    {
        [Export] Label nameLabel;
        [Export] Label ability;
        [Export] Label pingLabel;

        int lowPing = 40;
        int highPing = 100;
        Color lowPingColor = new(0, 1, 0);
        Color medPingColor = new(1, 1, 0);
        Color highPingColor = new(1, 0, 0);

        PlayerInfo playerInfo;

        public NetworkLobbySnake Init(PlayerInfo playerInfo)
        {
            this.playerInfo = playerInfo;

            nameLabel.Text = playerInfo.Name;
            nameLabel.AddThemeColorOverride("font_color", playerInfo.Color);
            
            ability.Text = GameManager.Instance.GetAbilityName(playerInfo.Ability);

            return this;
        }

        public void UpdatePing(float pingMs)
        {
            int ping = (int)pingMs;
            pingLabel.Text = ping.ToString() + "ms";
            pingLabel.AddThemeColorOverride("font_color", ping < lowPing ? lowPingColor : ping < highPing ? medPingColor : highPingColor);
        }
    }
}
