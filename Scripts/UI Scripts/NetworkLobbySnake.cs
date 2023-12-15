using ADK.Net;
using Godot;
using System;

namespace ADK.UI
{
    public partial class NetworkLobbySnake : HBoxContainer
    {
        [Export] Label nameLabel;
        [Export] Label ability;

        PlayerInfo playerInfo;

        public NetworkLobbySnake Init(PlayerInfo playerInfo)
        {
            this.playerInfo = playerInfo;

            nameLabel.Text = playerInfo.Name;
            nameLabel.AddThemeColorOverride("font_color", playerInfo.Color);
            
            ability.Text = GameManager.Instance.GetAbilityName(playerInfo.Ability);

            return this;
        }
    }
}
