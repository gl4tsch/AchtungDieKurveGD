using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ADK
{
    public partial class ScoreTracker
    {
        List<Snake> snakes;

        /// <summary>
        /// sorted descending
        /// </summary>
        List<SnakeScore> sortedScores = new();
        /// <summary>
        /// sorted descending
        /// </summary>
        public ReadOnlyCollection<SnakeScore> SortedScores => sortedScores.AsReadOnly();

        /// <summary>
        /// sorted descending
        /// </summary>
        public event Action<ReadOnlyCollection<SnakeScore>> ScoresChanged;

        public ScoreTracker(List<Snake> snakes)
        {
            this.snakes = snakes;
            foreach (var snake in snakes)
            {
                sortedScores.Add(new SnakeScore(snake, 0));
                snake.Died += OnSnakeDeath;
            }
            SortScores();
            GameManager.Instance.ActiveArenaScene.BattleStateChanged += OnBattleStateChanged;
        }

        void OnBattleStateChanged(ArenaScene.BattleState battleState)
        {
            if (battleState == ArenaScene.BattleState.StartOfRound)
            {
                // reset ability uses
                foreach (var snake in snakes)
                {
                    if (snake.Ability != null)
                    {
                        snake.Ability.Uses = sortedScores.Find(ss => ss.Snake == snake).Place;
                    }
                }
            }
        }

        void OnSnakeDeath(Snake snake)
        {
            // this includes the snake that just died
            int deadSnakeCount = snakes.Count(s => !s.IsAlive);
            sortedScores.First(s => s.Snake == snake).Score += deadSnakeCount - 1;
            SortScores();
        }

        void SortScores()
        {
            // sort descending
            sortedScores.Sort((a, b) => b.Score - a.Score);

            // update places
            int place = 1;
            // this goes from first to last
            for (int i = 0; i < SortedScores.Count; i++)
            {
                // increase place if score is not the same as the one before
                if (i > 0 && SortedScores[i-1].Score > SortedScores[i].Score)
                {
                    place++;
                }
                SortedScores[i].Place = place;
            }

            ScoresChanged?.Invoke(sortedScores.AsReadOnly());
        }
    }
}