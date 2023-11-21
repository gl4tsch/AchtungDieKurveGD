using System;

namespace ADK
{
    public class SnakeScore
    {
        public SnakeScore(Snake snake, int score)
        {
            Snake = snake;
            Score = score;
        }
        public Snake Snake {get; private set;}
        int score;
        public int Score
        {
            get => score;
            set
            {
                score = value;
                ScoreChanged?.Invoke(score);
            }
        }
        
        /// <summary>
        /// Set by ScoreTracker.
        /// This is used because more than one snake can share a place
        /// if the have the same score
        /// [1,numSnakes]
        /// </summary>
        public int Place {get; set;} = 1;
        public Action<int> ScoreChanged;
        public int AbilityUses => Snake?.Ability?.Uses ?? 0;
    }
}