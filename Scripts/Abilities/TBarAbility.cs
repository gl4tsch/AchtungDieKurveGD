using Godot;
using System;

namespace ADK
{
    public class TBarAbility : Ability
    {
        public static string DisplayName => "TBar";
        public override string Name => DisplayName;

        float halfBarLength = 60;
        
        public override void Activate(Snake snake)
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
        }
    }
}
