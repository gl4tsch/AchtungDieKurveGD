
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

        public TeleportAbility(SettingsSection settings) : base(settings){}

        public static Dictionary<string, Variant> DefaultSettings => new()
        {
            {distanceSettingKey, 80}
        };

        public override void ApplySettings(SettingsSection settings)
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