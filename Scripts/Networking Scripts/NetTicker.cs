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
        [Export] bool simulateLag = false;
        [Export] Key lagToggleKey = Key.Comma;
        [Export] Key packetLossKey = Key.Period;
        [Export] float minLagMs = 16f;
        [Export] float maxLagMs = 200f;
        SortedList<DateTime, ClientTickMessage> delayedClientMessages = new();
        SortedList<DateTime, ServerTickMessage> delayedServerMessages = new();
        bool lagging = false;
        bool loosingPackets = false;
        RandomNumberGenerator rng = new();

        #region AllClients
        List<long> sortedPlayerIds;
        int numPlayers => sortedPlayerIds.Count;
        SortedList<int, ISerializableInput[]> inputBuffer = new();
        int nextExpectedTick = 0;
        ISerializableInput localInput;
        List<int> receivedServerTicksToAcknowledge = new();
        int localTick = 0;
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

        public override void _Process(double delta)
        {
            if (!simulateLag) return;

            while (delayedClientMessages.Count > 0 && DateTime.Compare(delayedClientMessages.Keys[0], DateTime.Now) <= 0)
            {
                var key = delayedClientMessages.Keys[0];
                RpcId(1, nameof(ReceiveClientTickOnServer), delayedClientMessages[key].ToMessage());
                delayedClientMessages.Remove(key);
            }

            while (delayedServerMessages.Count > 0 && DateTime.Compare(delayedServerMessages.Keys[0], DateTime.Now) <= 0)
            {
                var key = delayedServerMessages.Keys[0];
                var message = delayedServerMessages[key];
                RpcId(message.Receiver, nameof(ReceiveServerTickOnClients), message.ToMessage());
                delayedServerMessages.Remove(key);
            }
        }

        /// <param name="localInput">the input to be sent to the server</param>
        /// <returns>the input for all players (by id) received from the server</returns>
        public Dictionary<long, ISerializableInput> Tick(ISerializableInput localInput)
        {
            // clients send their input and acknowledge received input
            SendClientTickMessage(localInput);

            // server broadcasts collected input
            if (Multiplayer.IsServer())
            {
                SendServerTickMessage();
            }

            Dictionary<long, ISerializableInput> consumedInput = new();
            // consume input buffer
            if (inputBuffer.ContainsKey(nextExpectedTick))
            {
                for (int i = 0; i < inputBuffer[nextExpectedTick].Length; i++)
                {
                    consumedInput.Add(sortedPlayerIds[i], inputBuffer[nextExpectedTick][i]);
                }
                inputBuffer.Remove(nextExpectedTick);
                nextExpectedTick++;
            }
            else // no input received from server since the last tick
            {
                GD.Print($"no input available for expected tick {nextExpectedTick}");
            }
            localTick++;
            return consumedInput;
        }

        void SendClientTickMessage(ISerializableInput input)
        {
            ClientTickMessage clientTick = new(input, receivedServerTicksToAcknowledge.ToArray());

            GD.Print($"{Multiplayer.GetUniqueId()}:\nSending input {clientTick.Input}\nand sending acknowledgements for ticks {string.Join(",", clientTick.AcknowledgedServerTicks)}");

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
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
        void ReceiveClientTickOnServer(byte[] clientTickData)
        {
            ClientTickMessage clientTick = new(clientTickData, inputSerializer);
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

                ServerTickMessage serverTick = new(GetPendingInputsForPlayer(id), id);
                if (simulateLag && loosingPackets)
                {
                    GD.Print($"Simulated packet loss for server tick {localTick} to client {id}");
                }
                else if (simulateLag && lagging)
                {
                    QueueDelayedMessage(serverTick);
                }
                else
                {
                    RpcId(id, nameof(ReceiveServerTickOnClients), serverTick.ToMessage());
                }
            }
            // do not reset input block between sends. this way, the previous input will be used if no input arrived from a client in time
        }

        [Rpc(MultiplayerApi.RpcMode.AnyPeer, CallLocal = true, TransferMode = MultiplayerPeer.TransferModeEnum.Unreliable)]
        void ReceiveServerTickOnClients(byte[] serverTickData)
        {
            ServerTickMessage serverTick = new(serverTickData, numPlayers, inputSerializer);

            // do not send acknowledgements again for ticks no longer received from the server
            receivedServerTicksToAcknowledge.Clear();

            foreach (var inputTick in serverTick.ClientsInputData)
            {
                if (inputBuffer.TryAdd(inputTick.Key, inputTick.Value))
                {
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
                    delayedServerMessages.Add(delayedTime, serverTick);
                    break;
                case ClientTickMessage clientTick:
                    delayedClientMessages.Add(delayedTime, clientTick);
                    break;
            }
        }
    }
}
