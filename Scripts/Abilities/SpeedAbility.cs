using Godot;
using System.Collections.Generic;

namespace ADK
{
    public class SpeedAbility : Ability
    {
        public static string DisplayName = "Speed";
        public override string Name => DisplayName;

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

        public override void Activate(Snake snake)
        {
            GD.Print("Mopsgeschwindigkeit!");

            this.snake = snake;
            snake.MoveSpeedModifier += speedModifier;
            snake.TurnRateModifier += turnRateModifier;
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
            snake.TurnRateModifier -= turnRateModifier;
        }
    }
}