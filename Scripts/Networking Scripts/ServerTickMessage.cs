using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Godot;

namespace ADK.Net
{
    /// <summary>
    /// what the server sends to each client every tick
    /// </summary>
    public class ServerTickMessage
    {
        // send all input not acknowledged by the client yet
        public SortedList<int, ISerializableInput[]> ClientsInputData = new();

        public ServerTickMessage(SortedList<int, ISerializableInput[]> clientsInputData)
        {
            ClientsInputData = clientsInputData;
        }

        /// <param name="tickData">of the form [tickNumber,[InputFlagsForEachPlayer],...]</param>
        public ServerTickMessage(byte[] tickData, int numPlayers, InputSerializer inputSerializer)
        {
            int sizeofInput = inputSerializer.SizeofInput;
            int dataBlockLength = sizeof(int) + numPlayers * sizeofInput;
            for (int i = 0; i < tickData.Length; i += dataBlockLength)
            {
                int tick = BitConverter.ToInt32(tickData.Take(i * sizeof(int)).ToArray());

                byte[] inputs = tickData[(i+1)..(i+dataBlockLength-1)];
                List<ISerializableInput> tickInputs = new();
                for (int j = 0; j < numPlayers; j++)
                {
                    int offset = j * sizeofInput;
                    byte[] inputBytes = inputs[offset..(offset + sizeofInput)];
                    tickInputs.Add(inputSerializer.DeserializeInput(inputBytes));
                }
                ClientsInputData.Add(tick, tickInputs.ToArray());
            }
        }

        public byte[] ToMessage(InputSerializer inputSerializer)
        {
            List<byte> data = new();
            foreach (var entry in ClientsInputData)
            {
                data.AddRange(BitConverter.GetBytes(entry.Key));
                foreach (var input in entry.Value)
                {
                    data.AddRange(inputSerializer.SerializeInput(input));
                }
            }
            return data.ToArray();
        }
    }
}
