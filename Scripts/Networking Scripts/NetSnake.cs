using Godot;

namespace ADK.Net
{
    /// <summary>
    /// right now it looks like this class is not needed
    /// </summary>
    public class NetSnake : Snake
    {
        public NetSnake(PlayerInfo player)
        {
            Name = player.Name;
            Color = player.Color;
            Ability = GameManager.Instance.CreateAbility(player.Ability);
        }
    }
}