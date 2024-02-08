using Godot;
using System;
using System.Collections.Generic;

namespace ADK
{
    public class SpeedAbility : Ability
    {
        public static string DisplayName = "Speed";
        public override string Name => DisplayName;

        static string speedSettingKey => $"{DisplayName}_{nameof(speedModifier)}";
        static string turnSettingKey => $"{DisplayName}_{nameof(turnRadiusModifier)}";
        static string durationSettingKey => $"{DisplayName}_{nameof(duration)}";

        float speedModifier = 2f;
        float turnRadiusModifier = 1f;
        float duration = 1.5f; // [s]

        Snake snake;
        List<DateTime> speedStartTimes = new();

        public SpeedAbility(SettingsSection settings) : base(settings){}

        public static Dictionary<string, Variant> DefaultSettings => new()
        {
            {speedSettingKey, 2f},
            {turnSettingKey, 1f},
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
                turnRadiusModifier = (float)turnSetting;
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
            snake.MoveSpeedModifier *= speedModifier;
            snake.TurnRadiusModifier *= turnRadiusModifier;
            speedStartTimes.Add(DateTime.Now);
            AudioManager.Instance?.PlaySound(SFX.SpeedAbility);
        }

        public override void Tick(float deltaT)
        {
            base.Tick(deltaT);

            for (int i = speedStartTimes.Count - 1; i >= 0; i--)
            {
                DateTime time = speedStartTimes[i];
                if (DateTime.Now.CompareTo(time + TimeSpan.FromSeconds(duration)) > 0)
                {
                    speedStartTimes.RemoveAt(i);
                    OnBoostEnd();
                }
            }
        }

        void OnBoostEnd()
        {
            GD.Print("Speed End");
            snake.MoveSpeedModifier /= speedModifier;
            snake.TurnRadiusModifier /= turnRadiusModifier;
        }

        public override void Cancel()
        {
            base.Cancel();
            foreach (DateTime _ in speedStartTimes)
            {
                OnBoostEnd();
            }
            speedStartTimes.Clear();
        }
    }
}