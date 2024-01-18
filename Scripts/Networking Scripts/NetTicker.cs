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
        #region AllClients
        List<long> sortedPlayerIds;
        int numPlayers => sortedPlayerIds.Count;
        SortedList<int, ISerializableInput[]> inputBuffer = new();
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

        public void Tick(ISerializableInput localInput)
        {
            // clients send their input and acknowledge received input
            SendClientTickMessage(localInput);

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

        void SendClientTickMessage(ISerializableInput input)
        {
            ClientTickMessage clientTick = new(input, receivedServerTicksToAcknowledge.ToArray());

            GD.Print($"{Multiplayer.GetUniqueId()}:\nSending input {clientTick.Input}\nand sending acknowledgements for ticks {string.Join(",", clientTick.AcknowledgedServerTicks)}");

            // send local input to server
            RpcId(1, nameof(ReceiveClientTickOnServer), clientTick.ToMessage());
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

                ServerTickMessage serverTick = new(GetPendingInputsForPlayer(id));
                RpcId(id, nameof(ReceiveServerTickOnClients), serverTick.ToMessage());
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
    }
}
