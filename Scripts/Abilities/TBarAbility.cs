using Godot;
using System;
using System.Collections.Generic;

namespace ADK
{
    public class TBarAbility : Ability
    {
        public static string DisplayName => "T-Bar";
        public override string Name => DisplayName;

        static string lengthSettingKey => $"{DisplayName}_{nameof(halfBarLength)}";
        float halfBarLength = 80;

        public TBarAbility(SettingsSection settings) : base(settings){}

        public static Dictionary<string, Variant> DefaultSettings => new()
        {
            {lengthSettingKey, 80}
        };

        public override void ApplySettings(SettingsSection settings)
        {
            if (settings.Settings.TryGetValue(lengthSettingKey, out Variant lengthSetting))
            {
                halfBarLength = (float)lengthSetting;
            }
        }

        protected override void Perform(Snake snake)
        {
            GD.Print("Activating " + Name);
            Vector2 barCenter = snake.PxPosition - snake.Direction * snake.PxThickness * 0.5f;
            Vector2 barLeft = barCenter + snake.Direction.Rotated(Mathf.DegToRad(-90)) * halfBarLength;
            Vector2 barRight = barCenter + snake.Direction.Rotated(Mathf.DegToRad(90)) * halfBarLength;
            var bar = new LineData()
            {
                prevPosX = barLeft.X,
                prevPosY = barLeft.Y,
                newPosX = barRight.X,
                newPosY = barRight.Y,
                halfThickness = snake.PxThickness / 2,
                colorR = snake.Color.R,
                colorG = snake.Color.G,
                colorB = snake.Color.B,
                colorA = snake.Color.A,
                clipMode = 0
            };
            snake.InjectDrawData(new(){bar});
            AudioManager.Instance?.PlaySound(SFX.BarAbility);
        }
    }
}
