using Godot;
using System;
using ADK;

namespace ADK.UI
{
    public partial class ScoreBoardEntry : Control
    {
        [Export] Label ScoreLabel, AbilityUsesLabel;

        SnakeScore snakeScore;
        public SnakeScore SnakeScore => snakeScore;

        public ScoreBoardEntry Init(SnakeScore snakeScore)
        {
            this.snakeScore = snakeScore;

            if (snakeScore.Snake.Ability != null)
            {
                snakeScore.Snake.Ability.UsesChanged += OnAbilityUsesChanged;
            }
            snakeScore.ScoreChanged += OnScoreChanged;
            OnAbilityUsesChanged(snakeScore.AbilityUses);
            OnScoreChanged(snakeScore.Score);

            ScoreLabel.AddThemeColorOverride("font_color", snakeScore.Snake.Color);
            AbilityUsesLabel.AddThemeColorOverride("font_color", snakeScore.Snake.Color);

            return this;
        }

        public override void _ExitTree()
        {
            base._ExitTree();
            
            // this has not been Init() yet. nothing to clean up
            if (snakeScore == null)
            {
                return;
            }

            if (snakeScore.Snake.Ability != null)
            {
                snakeScore.Snake.Ability.UsesChanged -= OnAbilityUsesChanged;
            }
            snakeScore.ScoreChanged -= OnScoreChanged;
        }

        void OnAbilityUsesChanged(int uses)
        {
            AbilityUsesLabel.Text = uses.ToString();
        }

        void OnScoreChanged(int score)
        {
            ScoreLabel.Text = score.ToString();
        }
    }
}
    
