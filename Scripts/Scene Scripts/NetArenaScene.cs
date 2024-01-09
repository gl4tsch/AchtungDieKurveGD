using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ADK.Net
{
    public partial class NetArenaScene : Node
    {
#region AllClients
        Dictionary<long, NetSnake> snakes = new();
        List<long> sortedPlayerIds;
        Snake localSnake => GameManager.Instance.Snakes[0];

        SortedList<int, InputFlags[]> inputBuffer = new();
        InputFlags localInput;
        List<int> receivedServerTicksToAcknowledge = new();
        int localTick = 0;
#endregion

#region ServerOnly
        // input from all clients gets collected here every tick
        InputFlags[] serverTickInputBlock;
        // by tickNumber
        SortedList<int, InputFlags[]> inputHistory = new();
        // keys = player id. value = list of tick numbers
        Dictionary<long, List<int>> pendingAcknowledgements = new();
#endregion

        public override void _Ready()
        {
            base._Ready();
            sortedPlayerIds = NetworkManager.Instance.Players.Keys.OrderBy(id => id).ToList();
            sortedPlayerIds.ForEach(id => pendingAcknowledgements.Add(id, new()));
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

            CollectLocalInput();
            
            // clients send their input and acknowledge received input
            SendClientTickMessage();

            // server broadcasts collected input
            if (Multiplayer.IsServer())
            {
                SendServerTickMessage();
            }

            // consume input buffer
            if (inputBuffer.ContainsKey(localTick))
            {
                // TODO consume
                inputBuffer.Remove(localTick);
                // increment tick counter
                localTick++;
            }
        }

        void CollectLocalInput()
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
                Input = localInput,
                AcknowledgedServerTicks = receivedServerTicksToAcknowledge.ToArray()
            };

            GD.Print($"Sending input {clientTick.Input}\nand sending acknowledgements for ticks {string.Join(",", clientTick.AcknowledgedServerTicks)}");
            
            // send local input to server
            RpcId(1, nameof(ReceiveClientTickOnServer), clientTick.ToMessage());
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
        void ReceiveClientTickOnServer(int[] clientTickData)
        {
            ClientTickMessage clientTick = new(clientTickData);
            int playerId = Multiplayer.GetRemoteSenderId();

            GD.Print($"Server received input {clientTick.Input} from Player {playerId}");
            serverTickInputBlock[PlayerIdToIdx(playerId)] = clientTick.Input;

            // all ticks acknowledged by the client will not be sent anymore
            pendingAcknowledgements[playerId].RemoveAll(tick => clientTick.AcknowledgedServerTicks.Contains(tick));
            // shorten input history if possible
        }

        void SendServerTickMessage()
        {
            inputHistory.Add(localTick, serverTickInputBlock);

            foreach (var id in sortedPlayerIds)
            {
                // always send input for the current server tick
                pendingAcknowledgements[id].Add(localTick);

                ServerTickMessage serverTick = new()
                {
                    ClientInputs = GetPendingInputsForPlayer(id)
                };
                RpcId(id, nameof(ReceiveServerTickOnClients), serverTick.ToMessage());
            }

            // do not reset input block between sends. this way, the previous input will be used if no input arrived from a client in time
        }

        SortedList<int, InputFlags[]> GetPendingInputsForPlayer(long playerId)
        {
            SortedList<int, InputFlags[]> inputs = new();
            foreach (var tick in pendingAcknowledgements[playerId])
            {
                inputs.Add(tick, inputHistory[tick]);
            }
            return inputs;
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
        void ReceiveServerTickOnClients(int[] serverTickData)
        {
            ServerTickMessage serverTick = new(serverTickData, sortedPlayerIds.Count);

            // do not send acknowledgements again for ticks no longer received from the server
            receivedServerTicksToAcknowledge.Clear();

            foreach (var inputTick in serverTick.ClientInputs)
            {
                if (inputBuffer.TryAdd(inputTick.Key, inputTick.Value))
                {
                    receivedServerTicksToAcknowledge.Add(inputTick.Key);
                }
                // else it is an input repeated by sliding window. ignore.
            }
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