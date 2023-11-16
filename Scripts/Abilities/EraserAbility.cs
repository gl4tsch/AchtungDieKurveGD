using Godot;
using System;

namespace ADK
{
    public class EraserAbility : Ability
    {
        public static string DisplayName => "Eraser";
        public override string Name => DisplayName;

        float eraserLength = 80;
        
        public override void Activate(Snake snake)
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
            snake.InjectDrawData(new(){ eraseLine });
        }
    }
}
