using Godot;
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

        // on all clients
        SortedList<int, InputFlags[]> inputBuffer = new();

        // server only
        // input from all clients gets collected here every tick
        InputFlags[] serverTickInputBlock;
        SortedList<int, InputFlags[]> inputHistory;
        Dictionary<int, int> highestAcknowledgedInputBlockPerPlayer = new();

        InputFlags localInput;
        List<int> receivedServerTicksToAcknowledge = new();
        int localTick = 0;
        int nextExpectedServerTick = 0;

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

        /// <summary>
        /// TICK
        /// </summary>
        public override void _PhysicsProcess(double delta)
        {
            base._PhysicsProcess(delta);

            // handle local input
            HandleLocalInput();
            
            // clients send their input and acknowledge received input
            SendClientTickMessage();

            // server broadcasts collected input
            if (Multiplayer.IsServer())
            {
                SendServerTickMessage();
            }

            // process input buffer

            // increment tick counter
            localTick++;
        }

        void HandleLocalInput()
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
            localInput = input;
        }

        void SendClientTickMessage()
        {
            ClientTickMessage clientTick = new()
            {
                AcknowledgedServerTicks = receivedServerTicksToAcknowledge.ToArray(),
                Input = localInput
            };

            GD.Print($"Sending input {clientTick.Input} and sending acknowledgements for ticks {string.Join(",", clientTick.AcknowledgedServerTicks)}");
            
            // send local input to server
            RpcId(1, nameof(ReceiveClientTickOnServer), clientTick.ToMessage());

            // do not send acknowledgements again for ticks no longer received from the server
            //receivedServerTicksToAcknowledge.RemoveAll();
        }

        /// <summary>
        /// there is no client prediction yet => the server does not wait for client input
        /// => so no confirmation has to be sent.
        /// if there is no input from a client for a given frame on the server,
        /// the input form the previous frame will be used
        /// </summary>
        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
        void ReceiveClientTickOnServer(int[] clientTickData)
        {
            ClientTickMessage clientTick = new(clientTickData);
            int playerId = Multiplayer.GetRemoteSenderId();

            GD.Print($"Server received input {clientTick.Input} from Player {playerId}");
            serverTickInputBlock[PlayerIdToIdx(playerId)] = clientTick.Input;

            // todo: handle acknowledgements
        }

        void SendServerTickMessage()
        {
            ServerTickMessage serverTick = new()
            {
                TickNumber = localTick,
                ClientInputs = serverTickInputBlock
            };

            GD.Print($"Broadcasting input block [{string.Join(" | ", serverTick.ClientInputs)}] for tick {serverTick.TickNumber}");

            Rpc(nameof(ReceiveServerTickOnClients), serverTick.ToMessage());

            // do not reset input block between sends. this way, the previous input will be used if no input arrived from a client in time
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
        void ReceiveServerTickOnClients(int[] serverTickData)
        {
            ServerTickMessage serverTick = new(serverTickData);
            GD.Print($"Received input block [{string.Join(" | ", serverTick.ClientInputs)}] from server");
            inputBuffer.Add(serverTick.TickNumber, serverTick.ClientInputs);
            receivedServerTicksToAcknowledge.Add(serverTick.TickNumber);
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
