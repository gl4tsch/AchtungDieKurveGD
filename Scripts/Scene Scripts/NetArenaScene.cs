using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ADK.Net
{
    public partial class NetArenaScene : Node
    {
        [Export] NetTicker netTicker;
        [Export] Arena arena;
        SnakeHandler snakeHandler;

        Dictionary<long, NetSnake> playerSnakes = new();
        Snake localSnake => GameManager.Instance.Snakes[0];
        SnakeInputSerializer inputSerializer = new();

        public override void _Ready()
        {
            base._Ready();
            snakeHandler = new(arena);
            var playerIDs = NetworkManager.Instance.Players.Keys.ToList();
            netTicker.Init(inputSerializer, playerIDs);
            playerIDs.ForEach(id => playerSnakes.Add(id, null));
            NetworkManager.Instance.AllReady += OnSceneLoadedForAllPlayers;
            NetworkManager.Instance.SendReady();
            GD.Print("Waiting for other Players...");
        }

        void OnSceneLoadedForAllPlayers()
        {
            GD.Print("All Ready!");

            // oneshot event. unsubscribe so the ready event can be reused later
            NetworkManager.Instance.AllReady -= OnSceneLoadedForAllPlayers;

            if (Multiplayer.IsServer())
            {
                var rng = new RandomNumberGenerator();
                Vector2 arenaSize = new(1024, 1024);
                Vector2 arenaCenter = arenaSize / 2;

                foreach (var player in NetworkManager.Instance.Players)
                {
                    var pxPosition = new Vector2(rng.RandfRange(0 + arenaSize.X / 4, arenaSize.X - arenaSize.X / 4), rng.RandfRange(0 + arenaSize.Y / 4, arenaSize.Y - arenaSize.Y / 4));
                    var direction = (arenaCenter - pxPosition).Normalized();
                    Rpc(nameof(SpawnSnakeForPlayer), player.Key, pxPosition, direction);
                }
            }
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        void SpawnSnakeForPlayer(long playerId, Vector2 position, Vector2 direction)
        {
            if (!playerSnakes.ContainsKey(playerId))
            {
                GD.PrintErr("received snake spawn rpc for unknown player: " + playerId);
            }
            var snake = new NetSnake(NetworkManager.Instance.Players[playerId], position, direction);
            // snake.PlayerId = playerId;
            playerSnakes[playerId] = snake;
            GD.Print($"Snake spawned at {position}, {direction}");

            if (playerSnakes.Values.All(s => s != null))
            {
                var snakes = playerSnakes.Values.Cast<Snake>().ToList();
                snakeHandler.SetSnakes(snakes);
                arena.Init(snakes.Count);
            }
        }

        /// <summary>
        /// TICK
        /// </summary>
        public override void _PhysicsProcess(double delta)
        {
            var collectedInput = CollectLocalInput();
            var input = netTicker.Tick(collectedInput);
            if (input.Count == 0)
            {
                GD.PrintErr("no input available. freezing simulation until it arrives");
            }
            else
            {
                HandleInput(input.Values.Cast<SnakeInput>().ToList());
                snakeHandler.UpdateSnakes(delta);
                // snakeHandler.HandleCollisions(); // wtf why does this not work
            }
        }

        public override void _Process(double delta)
        {

        }

        ISerializableInput CollectLocalInput()
        {
            // local input
            bool left = Input.IsKeyPressed(localSnake.TurnLeftKey);
            bool right = Input.IsKeyPressed(localSnake.TurnRightKey);
            bool fire = Input.IsKeyPressed(localSnake.FireKey);
            InputFlags input = InputFlags.None;
            if (left)
            {
                input |= InputFlags.Left;
            }
            if (right)
            {
                input |= InputFlags.Right;
            }
            if (fire)
            {
                input |= InputFlags.Fire;
            }
            return new SnakeInput(input);
        }

        public void HandleInput(List<SnakeInput> inputs)
        {
            snakeHandler.HandleSnakeInput(inputs);
        }

        void OnBattleStateChanged(ArenaScene.BattleState battleState)
        {
            if (battleState == ArenaScene.BattleState.StartOfRound)
            {
                arena.ResetArena();
                return;
            }
            else if (battleState == ArenaScene.BattleState.EndOfRound)
            {
                //EndRound();
                return;
            }
        }
    }
}
