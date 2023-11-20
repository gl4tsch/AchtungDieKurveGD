
namespace ADK
{
    public class TeleportAbility : Ability
    {
        public static string DisplayName = "Teleport";
        public override string Name => DisplayName;

        float teleportDistance = 80;

        public override void Activate(Snake snake)
        {
            snake.Teleport(snake.PxPosition + snake.Direction * teleportDistance);
            AudioManager.Instance?.PlaySound(SFX.TeleportAbility);
        }
    }
}