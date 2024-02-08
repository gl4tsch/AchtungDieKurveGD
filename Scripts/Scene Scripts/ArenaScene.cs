using ADK.UI;
using Godot;

namespace ADK
{
    public partial class ArenaScene : Node
    {
        [Export] protected Arena arena;
        [Export] protected ScoreBoard scoreBoard;
        [Export] protected PackedScene WinPopUpPrefab;

        public enum BattleState
        {
            Battle,
            EndOfRound
        }
        public BattleState CurrentBattleState { get; protected set; }

        protected EndOfRoundPopup popUpWindowInstance;
        protected SnakeHandler snakeHandler;
        protected ScoreTracker scoreTracker;

        public override void _Ready()
        {
            snakeHandler = new(arena);
            snakeHandler.SetSnakes(GameManager.Instance.Snakes);
            scoreTracker = new ScoreTracker(GameManager.Instance.Snakes);
            scoreBoard?.SetScoreTracker(scoreTracker);
            arena.Init(GameManager.Instance.Snakes.Count);
            AudioManager.Instance?.PlayMusic(Music.BattleTheme);
            StartNewRound();
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey keyEvent && !keyEvent.IsEcho())
            {
                snakeHandler.HandleSnakeInput(keyEvent);
                
                if (keyEvent.IsPressed())
                {
                    if (keyEvent.Keycode == Key.Escape)
                    {
                        // TODO pause game and open pause screen
                        GameManager.Instance.GoToScene(GameScene.Lobby);
                    }
                    if (keyEvent.Keycode == Key.Enter && CurrentBattleState == BattleState.EndOfRound)
                    {
                        StartNewRound();
                    }   
                }
            }
        }

        public override void _Process(double delta)
        {
            var aliveSnakes = snakeHandler.AliveSnakes;
            // we have a winner
            if (aliveSnakes.Count <= 1)
            {
                EndRound(aliveSnakes.Count == 1 ? aliveSnakes[0] : null);
            }

            // but the last player may still move while the round ends
            snakeHandler.UpdateSnakes(delta);
        }

        public void StartNewRound()
        {
            popUpWindowInstance?.QueueFree();
            // kill the last remaining snake if there is one to update score
            snakeHandler.KillAll();
            snakeHandler.Reset();
            snakeHandler.SpawnSnakes();
            arena.ResetArena();
            scoreTracker.ResetAbilityUses();
            CurrentBattleState = BattleState.Battle;
        }

        public void EndRound(Snake winner = null)
        {
            if (CurrentBattleState == BattleState.EndOfRound)
            {
                // already ending
                return;
            }
            DisplayWinPopup(winner);
            CurrentBattleState = BattleState.EndOfRound;
        }

        void DisplayWinPopup(Snake winner)
        {
            var popUp = WinPopUpPrefab.Instantiate<EndOfRoundPopup>().PopUp(winner);
            if (popUp == null) return;
            popUpWindowInstance = popUp;
            AddChild(popUpWindowInstance);
        }
    }
}
