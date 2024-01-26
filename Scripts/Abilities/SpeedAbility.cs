using Godot;
using System.Collections.Generic;

namespace ADK
{
    public class SpeedAbility : Ability
    {
        public static string DisplayName = "Speed";
        public override string Name => DisplayName;

        static string speedSettingKey => $"{DisplayName}_{nameof(speedModifier)}";
        static string turnSettingKey => $"{DisplayName}_{nameof(turnRateModifier)}";
        static string durationSettingKey => $"{DisplayName}_{nameof(duration)}";

        float speedModifier = 1.5f;
        float turnRateModifier = 1.5f;
        float duration = 1.5f; // [s]

        Snake snake;
        List<Countdown> startedTimers = new();
        class Countdown
        {
            public Countdown(float t)
            {
                this.t = t;
            }
            public float t;
        }

        public SpeedAbility(SettingsSection settings) : base(settings){}

        public static Dictionary<string, Variant> DefaultSettings => new()
        {
            {speedSettingKey, 1.5f},
            {turnSettingKey, 1.5f},
            {durationSettingKey, 1.5f}
        };

        public override void ApplySettings(SettingsSection settings)
        {
            if (settings.Settings.TryGetValue(speedSettingKey, out Variant speedSetting))
            {
                speedModifier = (float)speedSetting;
            }
            if (settings.Settings.TryGetValue(turnSettingKey, out Variant turnSetting))
            {
                turnRateModifier = (float)turnSetting;
            }
            if (settings.Settings.TryGetValue(durationSettingKey, out Variant durationSetting))
            {
                duration = (float)durationSetting;
            }
        }

        protected override void Perform(Snake snake)
        {
            GD.Print("Mopsgeschwindigkeit!");

            this.snake = snake;
            snake.MoveSpeedModifier += speedModifier;
            snake.TurnRadiusModifier += turnRateModifier;
            startedTimers.Add(new Countdown(duration));
            AudioManager.Instance?.PlaySound(SFX.SpeedAbility);
        }

        public override void Tick(float deltaT)
        {
            base.Tick(deltaT);

            foreach (var countdown in startedTimers)
            {
                countdown.t -= deltaT;
                if (countdown.t <= 0)
                {
                    OnBoostEnd();
                }
            }
            startedTimers.RemoveAll(t => t.t <= 0);
        }

        void OnBoostEnd()
        {
            GD.Print("Speed End");
            snake.MoveSpeedModifier -= speedModifier;
            snake.TurnRadiusModifier -= turnRateModifier;
        }
    }
}