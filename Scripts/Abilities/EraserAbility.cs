using Godot;
using System;
using System.Collections.Generic;

namespace ADK
{
    public class EraserAbility : Ability
    {
        public static string DisplayName => "Eraser";
        public override string Name => DisplayName;

        static string lengthSettingKey => $"{DisplayName}_{nameof(eraserLength)}";
        float eraserLength = 160;

        public EraserAbility(AbilitySettings settings) : base(settings){}

        public static List<(string key, Variant setting)> DefaultSettings => new List<(string key, Variant setting)>
        {
            (lengthSettingKey, 160) 
        };

        public override void ApplySettings(AbilitySettings settings)
        {
            if (settings.Settings.TryGetValue(lengthSettingKey, out Variant lengthSetting))
            {
                eraserLength = (float)lengthSetting;
            }
        }

        protected override void Perform(Snake snake)
        {
            GD.Print("Activating " + Name);

            Vector2 eraseTarget = snake.PxPosition + snake.Direction * eraserLength;

            LineData eraseLine = new()
            {
                prevPosX = snake.PxPosition.X,
                prevPosY = snake.PxPosition.Y,
                newPosX = eraseTarget.X,
                newPosY = eraseTarget.Y,
                halfThickness = snake.PxThickness / 2,
                colorR = 0,
                colorG = 0,
                colorB = 0,
                colorA = 0,
                clipMode = 1
            };
            snake.InjectDrawData(new() { eraseLine });
            AudioManager.Instance?.PlaySound(SFX.EraserAbility);
            return;

            snake.RequestExplosion(new LineFilter()
            {
                startPosX = snake.PxPosition.X,
                startPosY = snake.PxPosition.Y,
                endPosX = eraseTarget.X,
                endPosY = eraseTarget.Y,
                halfThickness = snake.PxThickness / 2,
                clipMode = 1
            });
        }
    }
}
