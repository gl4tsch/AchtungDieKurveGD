using System.Collections.Generic;
using System.Linq;
using ADK.Net;
using Godot;

namespace ADK
{
    /// <summary>
    /// handles snake updates and lifecycles
    /// </summary>
    public class SnakeHandler
    {
        List<Snake> snakes = new();
        List<Snake> aliveSnakes = new();
        Arena arena;

        public SnakeHandler(Arena arena)
        {
            this.arena = arena;
        }

        public void SetSnakes(List<Snake> snakes)
        {
            this.snakes = snakes;
            Reset();
        }

        public void Reset()
        {
            aliveSnakes.Clear();
            foreach (Snake snake in snakes)
            {
                aliveSnakes.Add(snake);
            }
        }

        public void UpdateSnakes(double deltaT)
        {
            aliveSnakes.RemoveAll(s => !s.IsAlive);
            // we have a winner
            if (aliveSnakes.Count <= 1)
            {
                GameManager.Instance?.ActiveArenaScene?.EndRound(aliveSnakes.Count == 1 ? aliveSnakes[0] : null);
            }
            // but the last player may still move while the round ends
            if (aliveSnakes.Count == 0)
            {
                return;
            }

            foreach (var snake in aliveSnakes)
            {
                snake.Update((float) deltaT);
            }

            List<Snake> collidedSnakes = arena.DrawSnakesAndLines(aliveSnakes);
            HandleCollisions(collidedSnakes);
            HandleSnakeExplosionRequests();
        }

        public void HandleSnakeInput(InputEventKey keyEvent)
        {
            foreach (Snake snake in snakes)
            {
                snake.HandleInput(keyEvent);
            }
        }

        public void HandleSnakeInput(List<SnakeInput> inputs)
        {
            for (int i = 0; i < inputs.Count; i++)
            {
                SnakeInput input = inputs[i];
                snakes[i].HandleInput(input);
            }
        }

        public void HandleCollisions(List<Snake> collidedSnakes)
        {
            // out of bounds fallback
            foreach (var snake in aliveSnakes.Except(collidedSnakes))
            {
                if (snake.PxPosition.X < 0 || snake.PxPosition.X >= arena.Width || snake.PxPosition.Y < 0 || snake.PxPosition.Y >= arena.Height)
                {
                    collidedSnakes.Add(snake);
                }
            }

            // work on separate list because OnCollision removes snake from aliveSnakes
            foreach (var snake in collidedSnakes)
            {
                snake.OnCollision();
            }
        }

        void HandleSnakeExplosionRequests()
        {
            List<LineFilter> explosionData = new();
            foreach (var snake in snakes)
            {
                explosionData.AddRange(snake.GetExplosionData());
            }
            foreach (var line in explosionData)
            {
                arena.ExplodePixels(line);
            }
        }
    }
}