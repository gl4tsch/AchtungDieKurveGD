using Godot;
using System;
using System.Collections.Generic;

namespace ADK.Net
{
    public partial class NetArenaScene : Node
    {
        Dictionary<long, NetSnake> snakes = new();

        public override void _Ready()
        {
            base._Ready();

            GameManager.Instance.Snakes.Clear();
            NetworkManager.Instance.AllReady += OnAllPlayersReady;
            NetworkManager.Instance.SendReady();
            GD.Print("Waiting for other Players...");
        }

        void OnAllPlayersReady()
        {
            GD.Print("All Ready!");
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
            if (snakes.ContainsKey(playerId))
            {
                snakes.Remove(playerId);
            }
            var snake = new NetSnake(NetworkManager.Instance.Players[playerId], position, direction);
            snakes.Add(playerId, snake);
            GD.Print($"Snake spawned at {position}, {direction}");
        }
    }
}
