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
        List<Snake> collidedSnakes = new();
        public List<Snake> CollidedSnakes => collidedSnakes;
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

        /// <summary>
        /// after executing this, the collided snakes list is filled and usable
        /// </summary>
        public void UpdateSnakes(double deltaT, bool handleCollisionsImmediately = true)
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

            collidedSnakes = arena.DrawSnakesAndLines(aliveSnakes);
            // add snakes out of bounds (e.g. the ones that jumped over the arena border)
            collidedSnakes.AddRange(CheckOutOfBounds(aliveSnakes).Except(collidedSnakes));

            if (handleCollisionsImmediately)
            {
                HandleCollisions(collidedSnakes);
            }
        }

        List<Snake> CheckOutOfBounds(List<Snake> snakes)
        {
            List<Snake> oobSnakes = new();
            foreach (var snake in snakes)
            {
                if (snake.PxPosition.X < 0 || snake.PxPosition.X >= arena.Width || snake.PxPosition.Y < 0 || snake.PxPosition.Y >= arena.Height)
                {
                    oobSnakes.Add(snake);
                }
            }
            return oobSnakes;
        }

        public void HandleCollisions()
        {
            HandleCollisions(collidedSnakes);
        }

        public void HandleCollisions(List<Snake> collidedSnakes)
        {
            foreach (var snake in collidedSnakes)
            {
                snake.OnCollision();
                // aliveSnakes.Remove(snake);
            }

            HandleSnakeExplosionRequests();
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