using Godot;
using System;
using System.Collections.Generic;

namespace ADK
{
    public class VBarAbility : Ability
    {
        public static string DisplayName => "V-Bar";
        public override string Name => DisplayName;

        static string lengthSettingKey => $"{DisplayName}_{nameof(halfBarLength)}";
        float halfBarLength = 80;

        public VBarAbility(SettingsSection settings) : base(settings){}

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
            Vector2 barCenter = snake.PxPosition - snake.Direction * snake.PxThickness * 2f;
            Vector2 barLeft = barCenter + snake.Direction.Rotated(Mathf.DegToRad(-35)) * halfBarLength;
            Vector2 barRight = barCenter + snake.Direction.Rotated(Mathf.DegToRad(35)) * halfBarLength;
            var leftBar = new LineData()
            {
                prevPosX = barLeft.X,
                prevPosY = barLeft.Y,
                newPosX = barCenter.X,
                newPosY = barCenter.Y,
                halfThickness = snake.PxThickness / 2,
                colorR = snake.Color.R,
                colorG = snake.Color.G,
                colorB = snake.Color.B,
                colorA = snake.Color.A,
                clipMode = 0
            };
            var rightBar = new LineData()
            {
                prevPosX = barRight.X,
                prevPosY = barRight.Y,
                newPosX = barCenter.X,
                newPosY = barCenter.Y,
                halfThickness = snake.PxThickness / 2,
                colorR = snake.Color.R,
                colorG = snake.Color.G,
                colorB = snake.Color.B,
                colorA = snake.Color.A,
                clipMode = 0
            };
            snake.InjectDrawData(new() { leftBar, rightBar });
            AudioManager.Instance?.PlaySound(SFX.BarAbility);
        }
    }
}
