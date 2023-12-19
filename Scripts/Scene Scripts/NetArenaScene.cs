using Godot;
using Godot.NativeInterop;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ADK.Net
{
    public partial class NetArenaScene : Node
    {
        Dictionary<long, NetSnake> snakes = new();
        List<long> sortedPlayerIds;
        Snake localSnake => GameManager.Instance.Snakes[0];
        // on all peers
        Queue<InputFlags[]> inputBuffer = new();
        // input from all peers gets collected here every tick
        InputFlags[] serverTickInputBlock;

        public override void _Ready()
        {
            base._Ready();
            sortedPlayerIds = NetworkManager.Instance.Players.Keys.OrderBy(id => id).ToList();
            ResetServerInputBlock();
            NetworkManager.Instance.AllReady += OnSceneLoadedForAllPlayers;
            NetworkManager.Instance.SendReady();
            GD.Print("Waiting for other Players...");
        }

        void ResetServerInputBlock()
        {
            serverTickInputBlock = new InputFlags[NetworkManager.Instance.Players.Count];
        }

        long IdxToPlayerId(int idx)
        {
            return sortedPlayerIds[idx];
        }

        int PlayerIdToIdx(long playerId)
        {
            return sortedPlayerIds.IndexOf(playerId);
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
            if (snakes.ContainsKey(playerId))
            {
                snakes.Remove(playerId);
            }
            var snake = new NetSnake(NetworkManager.Instance.Players[playerId], position, direction);
            snakes.Add(playerId, snake);
            GD.Print($"Snake spawned at {position}, {direction}");
        }

        public override void _PhysicsProcess(double delta)
        {
            base._PhysicsProcess(delta);
            
            SendLocalInputToServer();

            // broadcast collected input
            if (Multiplayer.IsServer())
            {
                int[] inputBlock = serverTickInputBlock.Cast<int>().ToArray();
                GD.Print($"Broadcasting input block [{string.Join(" | ", serverTickInputBlock)}]");
                Rpc(nameof(BroadcastInputBlock), inputBlock);
                // get ready to receive new inputs
                ResetServerInputBlock();
            }
        }

        void SendLocalInputToServer()
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
            GD.Print($"Sending input {input} to Server");
            // send local input to server
            RpcId(1, nameof(UpdateInputOnServer), (int)input);
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
        void UpdateInputOnServer(InputFlags input)
        {
            int playerId = Multiplayer.GetRemoteSenderId();
            GD.Print($"Server received input {input} from Player {playerId}");
            serverTickInputBlock[PlayerIdToIdx(playerId)] = input;
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
        void BroadcastInputBlock(int[] inputBlock)
        {
            InputFlags[] inputs = inputBlock.Cast<InputFlags>().ToArray();
            GD.Print($"Received input block [{string.Join(" | ", inputs)}] from server");
            inputBuffer.Enqueue(inputs);
        }
    }

    [Flags]
    public enum InputFlags
    {
        None = 0,
        Left = 1 << 0,
        Right = 1 << 1,
        Fire = 1 << 2
    }
}
