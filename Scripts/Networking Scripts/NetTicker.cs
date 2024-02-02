using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace ADK.Net
{
    /// <summary>
    /// sends and receives tick messages
    /// unfortunately this needs to be a Node to use RPCs.
    /// net delay can be simulated
    /// </summary>
    public partial class NetTicker : Node
    {
        [Export] int delayTicks = 2;

        [ExportCategory("Lag Simulation")]
        [Export] bool simulateLag = false;
        public bool DoSimulateLag => simulateLag;
        [Export] Key lagToggleKey = Key.Comma;
        [Export] Key packetLossKey = Key.Period;
        [Export] float minLagMs = 16f;
        [Export] float maxLagMs = 200f;

        SortedList<DateTime, byte[]> delayedClientMessages = new();
        SortedList<DateTime, (long receiver, byte[] message)> delayedServerMessages = new();
        SortedList<DateTime, Action> delayedMethodCalls = new();

        bool lagging = false;
        bool loosingPackets = false;
        RandomNumberGenerator rng = new();

        #region AllClients
        List<long> sortedPlayerIds;
        int numPlayers => sortedPlayerIds.Count;
        // all received inputs not consumed yet
        SortedList<int, ISerializableInput[]> inputBuffer = new();

        // where we should be at with consumption
        int maxNextTickToConsume => localTick - delayTicks - 1;
        // where we are actually at with consumption
        int nextTickToConsume = 0;
        int localTick = 0;

        public ISerializableInput LocalInput { get; set; }
        List<int> receivedServerTicksToAcknowledge = new();
        #endregion

        #region ServerOnly
        // input from all clients gets collected here every tick
        ISerializableInput[] serverTickInputBlock;
        // input history sorted by tickNumber
        SortedList<int, ISerializableInput[]> inputHistory = new();
        // keys = player id. value = list of tick numbers
        Dictionary<long, List<int>> pendingAcknowledgements = new();
        #endregion

        InputSerializer inputSerializer;

        long IdxToPlayerId(int idx)
        {
            return sortedPlayerIds[idx];
        }

        int PlayerIdToIdx(long playerId)
        {
            return sortedPlayerIds.IndexOf(playerId);
        }

        public void Init(InputSerializer inputSerializer, List<long> playerIDs)
        {
            this.inputSerializer = inputSerializer;
            this.sortedPlayerIds = playerIDs.OrderBy(id => id).ToList();
            sortedPlayerIds.ForEach(id => pendingAcknowledgements.Add(id, new()));
            ResetServerInputBlock();
        }

        void ResetServerInputBlock()
        {
            serverTickInputBlock = new ISerializableInput[numPlayers];
            for (int i = 0; i < numPlayers; i++)
            {
                serverTickInputBlock[i] = new SnakeInput();
            }
        }

        public override void _Input(InputEvent @event)
        {
            if (!simulateLag) return;

            if (@event is InputEventKey keyEvent)
            {
                if (keyEvent.Pressed && !keyEvent.IsEcho() && keyEvent.Keycode == lagToggleKey)
                {
                    lagging = !lagging;
                    GD.PrintErr($"Lag simulation " + (lagging ? "on" : "off"));
                }

                if (keyEvent.Keycode == packetLossKey)
                {
                    if (keyEvent.Pressed && !keyEvent.IsEcho()) // button down
                    {
                        GD.PrintErr("Startig packet loss simulation now");
                    }
                    loosingPackets = keyEvent.Pressed;
                    if (keyEvent.IsReleased())
                    {
                        GD.PrintErr("Packet loss simulation stopped");
                    }
                }
            }
        }

        /// <summary>
        /// update the simulated delay queues
        /// </summary>
        public override void _Process(double delta)
        {
            if (!simulateLag) return;

            while (delayedClientMessages.Count > 0 && DateTime.Compare(delayedClientMessages.Keys[0], DateTime.Now) <= 0)
            {
                var key = delayedClientMessages.Keys[0];
                RpcId(1, nameof(ReceiveClientTickOnServer), delayedClientMessages[key]);
                delayedClientMessages.Remove(key);
            }

            while (delayedServerMessages.Count > 0 && DateTime.Compare(delayedServerMessages.Keys[0], DateTime.Now) <= 0)
            {
                var key = delayedServerMessages.Keys[0];
                var message = delayedServerMessages[key];
                RpcId(message.receiver, nameof(ReceiveServerTickOnClients), message.message);
                delayedServerMessages.Remove(key);
            }

            while (delayedMethodCalls.Count > 0 && DateTime.Compare(delayedMethodCalls.Keys[0], DateTime.Now) <= 0)
            {
                var key = delayedMethodCalls.Keys[0];
                delayedMethodCalls[key]?.Invoke();
                delayedMethodCalls.Remove(key);
            }
        }

        /// <param name="localInput">the input to be sent to the server</param>
        public void Tick(ISerializableInput localInput)
        {
            // clients send their input and acknowledge received input
            SendClientTickMessage(localInput);

            // server broadcasts collected input
            if (Multiplayer.IsServer())
            {
                SendServerTickMessage();
            }
            localTick++;
        }

        /// <returns>the input for all players (by id) received from the server</returns>
        public Queue<TickInputs> ConsumeAllReadyTicks()
        {
            Queue<TickInputs> ticks = new();
            if (nextTickToConsume > maxNextTickToConsume)
            {
                // we are ahead of the fixed delay. wait.
                return ticks;
            }

            if(!inputBuffer.ContainsKey(nextTickToConsume)) // the next input needed is not here yet
            {
                GD.Print($"no input available for expected tick {nextTickToConsume}");
            }

            // only actually consume input if we are sufficiently behind delay already
            while (inputBuffer.ContainsKey(nextTickToConsume) && nextTickToConsume <= maxNextTickToConsume)
            {
                SortedList<long, ISerializableInput> consumedInput = new();
                for (int i = 0; i < inputBuffer[nextTickToConsume].Length; i++)
                {
                    consumedInput.Add(sortedPlayerIds[i], inputBuffer[nextTickToConsume][i]);
                }
                ticks.Enqueue(new TickInputs(nextTickToConsume, consumedInput));
                inputBuffer.Remove(nextTickToConsume);
                nextTickToConsume++;
            }
            return ticks;
        }

        void SendClientTickMessage(ISerializableInput input)
        {
            ClientTickMessage clientTick = new(input, receivedServerTicksToAcknowledge.ToArray());

            GD.Print($"{Multiplayer.GetUniqueId()}: Sending input {clientTick.Input}\t and sending acknowledgements for ticks [{string.Join(",", clientTick.AcknowledgedServerTicks)}]");

            // send local input to server
            if (simulateLag && loosingPackets)
            {
                GD.Print($"Simulated packet loss for client tick {localTick} to server");
            }
            else if (simulateLag && lagging)
            {
                QueueDelayedMessage(clientTick);
            }
            else
            {
                RpcId(1, nameof(ReceiveClientTickOnServer), clientTick.ToMessage());
            }

            // do not send acknowledgements again for ticks no longer received from the server
            receivedServerTicksToAcknowledge.Clear();
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
        void ReceiveClientTickOnServer(byte[] clientTickData)
        {
            ClientTickMessage clientTick = new(clientTickData, inputSerializer);
            int playerId = Multiplayer.GetRemoteSenderId();

            GD.Print($"Server received input {clientTick.Input} from Player {playerId} for tick {localTick}");
            serverTickInputBlock[PlayerIdToIdx(playerId)] = clientTick.Input;

            // all ticks acknowledged by the client will not be sent anymore
            //pendingAcknowledgements[playerId].RemoveAll(tick => clientTick.AcknowledgedServerTicks.Contains(tick));
            //TODO: shorten input history if possible
        }

        void SendServerTickMessage()
        {
            ISerializableInput[] inputBlock = new ISerializableInput[serverTickInputBlock.Length];
            serverTickInputBlock.CopyTo(inputBlock, 0);
            GD.Print($"Server tick [{localTick}] input history addition: {string.Join(',', inputBlock.ToList())}");
            inputHistory.Add(localTick, inputBlock);

            foreach (var playerId in sortedPlayerIds)
            {
                // always send input for the current server tick
                pendingAcknowledgements[playerId].Add(localTick);

                ServerTickMessage serverTick = new(GetPendingInputsForPlayer(playerId), playerId);
                GD.Print($"Server sending unacknowledged ticks to {playerId}: {serverTick}");

                if (simulateLag && loosingPackets)
                {
                    GD.PrintErr($"Simulated packet loss for server tick {localTick} to client {playerId}");
                }
                else if (simulateLag && lagging)
                {
                    QueueDelayedMessage(serverTick);
                }
                else
                {
                    RpcId(playerId, nameof(ReceiveServerTickOnClients), serverTick.ToMessage());
                }
            }
            // do not reset input block between sends. this way, the previous input will be used if no input arrived from a client in time
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
        void ReceiveServerTickOnClients(byte[] serverTickData)
        {
            ServerTickMessage serverTick = new(serverTickData, numPlayers, inputSerializer);
            GD.Print($"{Multiplayer.GetUniqueId()}: received server message with ticks: {serverTick}");

            foreach (var inputTick in serverTick.ClientsInputData)
            {
                if (inputBuffer.TryAdd(inputTick.Key, inputTick.Value))
                {
                    GD.Print($"{Multiplayer.GetUniqueId()}: tick {inputTick.Key} is new");
                    receivedServerTicksToAcknowledge.Add(inputTick.Key);
                }
                // else it is an input repeated by sliding window. ignore.
            }
        }

        SortedList<int, ISerializableInput[]> GetPendingInputsForPlayer(long playerId)
        {
            SortedList<int, ISerializableInput[]> inputs = new();
            foreach (var tick in pendingAcknowledgements[playerId])
            {
                inputs.Add(tick, inputHistory[tick]);
            }
            return inputs;
        }

        void QueueDelayedMessage(INetworkMessage message)
        {
            float delay = rng.RandfRange(minLagMs, maxLagMs);
            DateTime delayedTime = DateTime.Now.AddMilliseconds(delay);

            switch (message)
            {
                case ServerTickMessage serverTick:
                    delayedServerMessages.Add(delayedTime, (serverTick.Receiver, serverTick.ToMessage()));
                    break;
                case ClientTickMessage clientTick:
                    delayedClientMessages.Add(delayedTime, clientTick.ToMessage());
                    break;
            }
        }

        public void SimulateDelayedRpc(Action method, float delayMs = -1)
        {
            if (!simulateLag || !lagging && !loosingPackets)
            {
                method?.Invoke();
                return;
            }

            if (delayMs < 0)
            {
                delayMs = rng.RandfRange(minLagMs, maxLagMs);
            }
            DateTime delayedTime = DateTime.Now.AddMilliseconds(delayMs);
            delayedMethodCalls.Add(delayedTime, method);
        }
    }
}
