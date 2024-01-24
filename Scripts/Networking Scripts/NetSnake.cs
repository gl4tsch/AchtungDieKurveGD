using Godot;

namespace ADK.Net
{
    public class NetSnake : Snake
    {
        public NetSnake(PlayerInfo player, Vector2 pos, Vector2 dir)
        {
            Name = player.Name;
            Color = player.Color;
            Ability = GameManager.Instance.CreateAbility(player.Ability);
            Spawn(pos, dir);
        }
    }
}