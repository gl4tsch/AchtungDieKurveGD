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

        Dictionary<long, NetSnake> snakes = new();
        Snake localSnake => GameManager.Instance.Snakes[0];
        SnakeInputSerializer inputSerializer = new();

        public override void _Ready()
        {
            base._Ready();
            var playerIDs = NetworkManager.Instance.Players.Keys.ToList();
            netTicker.Init(inputSerializer, playerIDs);
            playerIDs.ForEach(id => snakes.Add(id, null));
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
            if (!snakes.ContainsKey(playerId))
            {
                GD.PrintErr("received snake spawn rpc for unknown player: " + playerId);
            }
            var snake = new NetSnake(NetworkManager.Instance.Players[playerId], position, direction);
            snake.PlayerId = playerId;
            snakes[playerId] = snake;
            GD.Print($"Snake spawned at {position}, {direction}");

            if (snakes.Values.All(s => s != null))
            {
                arena.Init(snakes.Values.Cast<Snake>().ToList());
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
                arena.HandleInput(input.Values.Cast<SnakeInput>().ToList());
            }
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
    }
}
