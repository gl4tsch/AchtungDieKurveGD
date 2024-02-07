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
        public List<Snake> AliveSnakes => aliveSnakes;
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
        }

        public void Reset()
        {
            aliveSnakes.Clear();
            foreach (Snake snake in snakes)
            {
                aliveSnakes.Add(snake);
            }
        }

        public void KillAll()
        {
            foreach (var snake in aliveSnakes)
            {
                snake.Kill();
            }
            aliveSnakes.Clear();
        }

        public void SpawnSnakes()
        {
            foreach (var snake in snakes)
            {
                snake.Spawn((Vector2I)arena.Dimensions);
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
            collidedSnakes.Clear();
            aliveSnakes.RemoveAll(s => !s.IsAlive);
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