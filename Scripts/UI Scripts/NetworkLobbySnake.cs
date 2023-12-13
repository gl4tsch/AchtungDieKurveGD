using ADK.Net;
using Godot;
using System;

namespace ADK.UI
{
    public partial class NetworkLobbySnake : HBoxContainer
    {
        [Export] Label nameLabel;
        [Export] OptionButton abilityDD; 

        PlayerInfo playerInfo;

        public NetworkLobbySnake Init(PlayerInfo playerInfo)
        {
            this.playerInfo = playerInfo;

            nameLabel.Text = playerInfo.Name;
            abilityDD.Clear();
            foreach (var ability in GameManager.Instance.AbilityFactory)
            {
                abilityDD.AddItem(ability.name);
            }
            abilityDD.Select(playerInfo.Ability);

            return this;
        }
    }
}
