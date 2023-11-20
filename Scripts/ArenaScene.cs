using Godot;
using System;

namespace ADK
{
    public partial class ArenaScene : Node
    {
        public override void _Ready()
        {
            base._Ready();
            AudioManager.Instance?.PlayMusic(Music.BattleTheme);
        }

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);
            if (@event is InputEventKey keyEvent && keyEvent.IsPressed() && !keyEvent.IsEcho())
            {
                if (keyEvent.Keycode == Key.Escape)
                {
                    // pause game and open pause screen
                    GameManager.Instance.GoToScene(GameScene.Lobby);
                }
            }
        }
    }
}
