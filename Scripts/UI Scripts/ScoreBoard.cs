using Godot;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace ADK.UI
{
    public partial class ScoreBoard : Control
    {
        [Export] PackedScene snakeScorePrefab;
        [Export] Control snakeScoreContainer;

        ScoreTracker scoreTracker;
        List<ScoreBoardEntry> scoreInstances = new();

        public override void _Ready()
        {
            base._Ready();

            scoreTracker = new ScoreTracker(GameManager.Instance.Snakes);
            scoreTracker.ScoresChanged += OnScoresChanged;

            // clear old ui instances
            scoreInstances.Clear();
            foreach (var node in snakeScoreContainer.GetChildren())
            {
                node.Free();
            }

            // spawn new ui instances
            foreach (var score in scoreTracker.SortedScores)
            {
                ScoreBoardEntry instance = snakeScorePrefab.Instantiate<ScoreBoardEntry>().Init(score);
                snakeScoreContainer.AddChild(instance);
                scoreInstances.Add(instance);
            }
        }

        public override void _ExitTree()
        {
            base._ExitTree();
            scoreTracker.ScoresChanged -= OnScoresChanged;
        }

        void OnScoresChanged(ReadOnlyCollection<SnakeScore> snakeScores)
        {
            for (int i = 0; i < snakeScores.Count; i++)
            {
                var scoreInstance = scoreInstances.Find(s => s.SnakeScore == snakeScores[i]);
                snakeScoreContainer.MoveChild(scoreInstance, i);
            }
        }
    }
}
