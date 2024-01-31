using Godot;
using System;
using System.Linq;

namespace ADK
{
    public partial class ArenaScene : Node
    {
        [Export] Arena arena;
        [Export] PackedScene WinPopUpPrefab;

        public enum BattleState
        {
            StartOfRound,
            Battle,
            EndOfRound
        }
        public BattleState CurrentBattleState { get; private set; }
        public event Action<BattleState> BattleStateChanged;

        EndOfRoundPopup popUpWindowInstance;

        public override void _Ready()
        {
            base._Ready();
            arena.Init(GameManager.Instance.Snakes.Count);
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
            popUpWindowInstance?.QueueFree();
            BroadcastBattleStateTransition(BattleState.StartOfRound);
        }

        public void EndRound(Snake winner = null)
        {
            if (CurrentBattleState == BattleState.EndOfRound)
            {
                // already ending
                return;
            }
            DisplayWinPopup(winner);
            BroadcastBattleStateTransition(BattleState.EndOfRound);
        }

        void DisplayWinPopup(Snake winner)
        {
            popUpWindowInstance = WinPopUpPrefab.Instantiate<EndOfRoundPopup>().PopUp(winner);
            AddChild(popUpWindowInstance);
        }

        void BroadcastBattleStateTransition(BattleState state)
        {
            CurrentBattleState = state;
            BattleStateChanged?.Invoke(CurrentBattleState);
        }
    }
}
