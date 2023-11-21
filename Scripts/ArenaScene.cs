using Godot;
using System;
using System.Linq;

namespace ADK
{
    public partial class ArenaScene : Node
    {
        public enum BattleState
        {
            StartOfRound,
            Battle,
            EndOfRound
        }
        public BattleState CurrentBattleState { get; private set; }
        public event Action<BattleState> BattleStateChanged;

        public override void _Ready()
        {
            base._Ready();
            AudioManager.Instance?.PlayMusic(Music.BattleTheme);
            StartNewRound();
        }

        public override void _Input(InputEvent @event)
        {
            base._Input(@event);
            if (@event is InputEventKey keyEvent && keyEvent.IsPressed() && !keyEvent.IsEcho())
            {
                if (keyEvent.Keycode == Key.Escape)
                {
                    // TODO pause game and open pause screen
                    GameManager.Instance.GoToScene(GameScene.Lobby);
                }
                if (keyEvent.Keycode == Key.Enter && CurrentBattleState == BattleState.EndOfRound)
                {
                    // kill the last remaining snake if there is one to update score
                    foreach (var aliveSnake in GameManager.Instance.Snakes.Where(s => s.IsAlive))
                    {
                        aliveSnake.Kill();
                    }
                    StartNewRound();
                }
            }
        }

        public void StartNewRound()
        {
            BroadcastBattleStateTransition(BattleState.StartOfRound);
        }

        public void EndRound()
        {
            if (CurrentBattleState == BattleState.EndOfRound)
            {
                // already ending
                return;
            }
            // TODO display win/continue message or implement a win screen node
            // listening to battle state transition broadcast
            
            BroadcastBattleStateTransition(BattleState.EndOfRound);
        }

        void BroadcastBattleStateTransition(BattleState state)
        {
            CurrentBattleState = state;
            BattleStateChanged?.Invoke(CurrentBattleState);
        }
    }
}
