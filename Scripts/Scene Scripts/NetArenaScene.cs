using ADK.UI;
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
        [Export] ScoreBoard scoreBoard;
        SnakeHandler snakeHandler;
        ScoreTracker scoreTracker;

        SortedList<long, Snake> playerSnakes = new();
        Snake localSnake => GameManager.Instance.Snakes[0];
        SnakeInputSerializer inputSerializer = new();
        Queue<TickInputs> ticksToExecute = new();
        int snakeRespawnCounter = 0;
        bool readyToRumble = false;

        public override void _Ready()
        {
            snakeHandler = new(arena);
            var playerIDs = NetworkManager.Instance.Players.Keys.ToList();
            netTicker.Init(inputSerializer, playerIDs);
            InitializeSnakes();
            if (Multiplayer.IsServer())
            {
                NetworkManager.Instance.AllReadyOneshot += SendStartNewRound;
            }
            NetworkManager.Instance.SendReady();
            GD.Print("Waiting for other Players...");

            scoreTracker = new(playerSnakes.Values.ToList());
            scoreBoard.SetScoreTracker(scoreTracker);
            AudioManager.Instance?.PlayMusic(Music.BattleTheme);
        }

        void InitializeSnakes()
        {
            foreach (var player in NetworkManager.Instance.Players)
            {
                Ability ability = GameManager.Instance.CreateAbility(player.Value.Ability);
                playerSnakes.Add(player.Key, new Snake(player.Value.Name, player.Value.Color, ability));
            }
                
            var sortedSnakes = playerSnakes.Values.ToList();
            snakeHandler.SetSnakes(sortedSnakes);
            arena.Init(sortedSnakes.Count);
        }

        public override void _Input(InputEvent @event)
        {
            if (@event is InputEventKey keyEvent && keyEvent.IsPressed() && !keyEvent.IsEcho())
            {
                if (keyEvent.Keycode == Key.Escape)
                {
                }
                if (keyEvent.Keycode == Key.Enter && Multiplayer.IsServer())
                {
                    SendStartNewRound();
                }
            }
        }

        // server
        // send snake spawn positions once all players have finished loading the scene
        void SendStartNewRound()
        {
            GD.PrintErr("Server starting new round...");
            Rpc(nameof(ReceiveStartNewRound));
            NetworkManager.Instance.AllReadyOneshot += SendReadyToRumble;
            SendRespawnSnakes();
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        void ReceiveStartNewRound()
        {
            readyToRumble = false;
            netTicker.Reset();
            // kill the last remaining snake if there is one to update score
            snakeHandler.KillAll();
            snakeHandler.Reset();
            arena.ResetArena();
            scoreTracker.ResetAbilityUses();
        }

        // server
        void SendRespawnSnakes()
        {
            var rng = new RandomNumberGenerator();
            Vector2 arenaCenter = arena.Dimensions / 2f;

            foreach (var player in playerSnakes)
            {
                var pxPosition = new Vector2(rng.RandfRange(0 + arena.Width / 4, arena.Width - arena.Width / 4), rng.RandfRange(0 + arena.Height / 4, arena.Height - arena.Height / 4));
                var direction = (arenaCenter - pxPosition).Normalized();
                Rpc(nameof(ReceiveSnakeSpawn), player.Key, pxPosition, direction);
            }
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        void ReceiveSnakeSpawn(long playerId, Vector2 position, Vector2 direction)
        {
            if (!playerSnakes.ContainsKey(playerId))
            {
                GD.PrintErr("received snake spawn rpc for unknown player: " + playerId);
            }

            playerSnakes[playerId].Spawn(position, direction);
            GD.Print($"{Multiplayer.GetUniqueId()}: Snake spawned at {position}, {direction}");

            snakeRespawnCounter++;
            if (snakeRespawnCounter == playerSnakes.Count)
            {
                GD.Print($"{Multiplayer.GetUniqueId()} has spawned all snakes and is ready to rumble!");
                NetworkManager.Instance.SendReady();
                snakeRespawnCounter = 0;
            }
        }

        // server
        void SendReadyToRumble()
        {
            Rpc(nameof(ReceiveReadyToRumble));
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Reliable)]
        void ReceiveReadyToRumble()
        {
            GD.Print($"{Multiplayer.GetUniqueId()} received ready to rumble!");
            readyToRumble = true;
        }

        /// <summary>
        /// NET TICK
        /// </summary>
        public override void _PhysicsProcess(double delta)
        {
            if (!readyToRumble) return;

            var collectedInput = CollectLocalInput();
            netTicker.Tick(collectedInput);
            Queue<TickInputs> ticks = netTicker.ConsumeAllReadyTicks();
            if (ticks.Count == 0)
            {
                GD.PrintErr($"{Multiplayer.GetUniqueId()}: no input available. freezing simulation until it arrives");
            }
            while (ticks.Count > 0)
            {
                var tick = ticks.Dequeue();
                ticksToExecute.Enqueue(tick);
            }
        }

        public override void _Process(double delta)
        {
            if (ticksToExecute.TryDequeue(out TickInputs tick))
            {
                ExecuteTick(tick, 1f / Engine.PhysicsTicksPerSecond);
            }
        }

        void ExecuteTick(TickInputs inputs, double deltaT)
        {
            var orderedInputList = inputs.PlayersInput.Values.Cast<SnakeInput>().ToList();
            GD.PrintErr($"{Multiplayer.GetUniqueId()}: executing input for tick {inputs.TickNumber}: {string.Join(",", orderedInputList)}");
            snakeHandler.HandleSnakeInput(orderedInputList);
            snakeHandler.UpdateSnakes(deltaT, false);
            if (Multiplayer.IsServer() && snakeHandler.CollidedSnakes.Count > 0)
            {
                SendCollisionMessages(snakeHandler.CollidedSnakes);
            }
        }

        // server
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
        void ReceiveCollisionMessage(long collidedPlayer) // TODO: pass tick number and explode accordingly
        {
            snakeHandler.HandleCollisions(new(){playerSnakes[collidedPlayer]});
        }

        bool flipFlop = false;
        ISerializableInput DebugFlipFlopInput()
        {
            flipFlop = !flipFlop;
            InputFlags input = flipFlop ? InputFlags.Left : InputFlags.Right;
            return new SnakeInput(input);
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
    }
}
