using Godot;
using System;

namespace ADK
{
    [Tool]
    public partial class EndOfRoundPopup : Control
    {
        [Export] Label winnerName, winnerLabel;
        [Export] float popAnimationDuration = 0.8f;
        [Export] float textAnimationSpeed = 1f;
        [Export] bool doAnimateInEditor = true;

        float winnerHue = 0;
        float animatedHue = 0;
        float hueDodgeThresh = 0.1f;

        public override void _Ready()
        {
            if (Engine.IsEditorHint())
            {
                return;
            }

            base._Ready();

            PivotOffset = Size / 2;
            Scale = Vector2.Zero;
            Tween popTween = GetTree().CreateTween();
            popTween.SetEase(Tween.EaseType.Out);
            popTween.SetTrans(Tween.TransitionType.Bounce);
            popTween.TweenProperty(this, "scale", Vector2.One, popAnimationDuration);
        }

        public EndOfRoundPopup PopUp(Snake winner)
        {
            if (winner == null) return null;
            winnerHue = winner.Color.H;
            winnerName.Text = winner.Name;
            winnerName.AddThemeColorOverride("font_color", winner.Color);
            winnerLabel.AddThemeColorOverride("font_color", winner.Color);
            return this;
        }

        public override void _Process(double delta)
        {
            if (Engine.IsEditorHint() && !doAnimateInEditor)
            {
                return;
            }

            base._Process(delta);

            animatedHue += (float)delta * textAnimationSpeed;
            animatedHue = Mathf.PosMod(animatedHue, 1);
            // make sure the animated text outline does not have the exact same color as the text
            float bolsteredHue = animatedHue + hueDodgeThresh;
            bolsteredHue = Mathf.PosMod(bolsteredHue, 1);
            if (Mathf.Abs(animatedHue - winnerHue) < hueDodgeThresh ||
                Mathf.Abs(bolsteredHue - winnerHue) < hueDodgeThresh)
            {
                animatedHue = winnerHue + hueDodgeThresh;
            }

            if (winnerLabel != null)
            {
                winnerLabel.AddThemeColorOverride("font_outline_color", Color.FromHsv(animatedHue, 1, 1));
            }
        }
    }
}
