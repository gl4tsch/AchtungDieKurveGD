
using System.Collections.Generic;
using Godot;

namespace ADK
{
    public class TeleportAbility : Ability
    {
        public static string DisplayName = "Teleport";
        public override string Name => DisplayName;

        static string distanceSettingKey => $"{DisplayName}_{nameof(teleportDistance)}";
        float teleportDistance = 80;

        public TeleportAbility(AbilitySettings settings) : base(settings){}

        public static List<(string key, Variant setting)> DefaultSettings => new List<(string key, Variant setting)>
        {
            (distanceSettingKey, 80)
        };

        public override void ApplySettings(AbilitySettings settings)
        {
            if (settings.Settings.TryGetValue(distanceSettingKey, out Variant distanceSetting))
            {
                teleportDistance = (float)distanceSetting;
            }
        }

        protected override void Perform(Snake snake)
        {
            snake.Teleport(snake.PxPosition + snake.Direction * teleportDistance);
            AudioManager.Instance?.PlaySound(SFX.TeleportAbility);
        }
    }
}