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

        SortedList<long, NetSnake> playerSnakes = new();
        List<Snake> sortedSnakes; // sorted by player id from playerSnakes
        Snake localSnake => GameManager.Instance.Snakes[0];
        SnakeInputSerializer inputSerializer = new();
        Queue<TickInputs> ticksToExecute = new();

        public override void _Ready()
        {
            base._Ready();
            Engine.PhysicsTicksPerSecond = 3;
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
                sortedSnakes = playerSnakes.Values.Cast<Snake>().ToList();
                snakeHandler.SetSnakes(sortedSnakes);
                arena.Init(sortedSnakes.Count);
            }
        }

        /// <summary>
        /// NET TICK
        /// </summary>
        public override void _PhysicsProcess(double delta)
        {
            var collectedInput = CollectLocalInput();
            netTicker.Tick(collectedInput);
            var ticks = netTicker.ConsumeAllReadyTicks();
            if (ticks.Count == 0)
            {
                GD.PrintErr("no input available. freezing simulation until it arrives");
            }
            while (ticks.Count > 0)
            {
                var tick = ticks.Dequeue();
                ticksToExecute.Enqueue(tick);
            }
        }

        double t = 0;
        public override void _Process(double delta)
        {
            t += delta;
            if (t > 0.1f)
            {
                if (ticksToExecute.TryDequeue(out var tick))
                {
                    ExecuteTick(tick, 1f/3f);
                }
                t -= 0.1f;
            }
        }

        void ExecuteTick(TickInputs inputs, double deltaT)
        {
            var orderedInputList = inputs.PlayersInput.Values.Cast<SnakeInput>().ToList();
            snakeHandler.HandleSnakeInput(orderedInputList);
            snakeHandler.UpdateSnakes(deltaT, false);
            if (Multiplayer.IsServer() && snakeHandler.CollidedSnakes.Count > 0)
            {
                SendCollisionMessages(snakeHandler.CollidedSnakes);
            }
        }

        void SendCollisionMessages(List<Snake> collidedSnakes)
        {
            foreach (var snake in collidedSnakes)
            {
                long collidedPlayer = playerSnakes.FirstOrDefault(ps => ps.Value == snake).Key;
                foreach (var player in playerSnakes.Keys)
                {
                    if (netTicker.DoSimulateLag)
                    {
                        netTicker.SimulateDelayedRpc(() => RpcId(player, nameof(ReceiveCollisionMessage), collidedPlayer));
                    }
                    else
                    {
                        RpcId(player, nameof(ReceiveCollisionMessage), collidedPlayer);
                    }
                }
            }
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        void ReceiveCollisionMessage(long collidedPlayer)
        {
            snakeHandler.HandleCollisions(new(){playerSnakes[collidedPlayer]});
        }

        ISerializableInput CollectLocalInput()
        {
            // local input
            bool left = Input.IsPhysicalKeyPressed(localSnake.TurnLeftKey);
            bool right = Input.IsPhysicalKeyPressed(localSnake.TurnRightKey);
            bool fire = Input.IsPhysicalKeyPressed(localSnake.FireKey);
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
