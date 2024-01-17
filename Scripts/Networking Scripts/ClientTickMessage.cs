using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;

namespace ADK.Net
{
    /// <summary>
    /// what each client sends to the server every tick
    /// </summary>
    public partial class ClientTickMessage
    {
        public ISerializableInput Input;
        public int[] AcknowledgedServerTicks;

        /// <summary>
        /// an empty message
        /// </summary>
        public ClientTickMessage(ISerializableInput input, int[] acknowledgedServerTicks)
        {
            Input = input;
            AcknowledgedServerTicks = acknowledgedServerTicks;
        }

        /// <summary>
        /// decode message
        /// </summary>
        public ClientTickMessage(byte[] data, InputSerializer inputSerializer)
        {
            int sizeofInput = inputSerializer.SizeofInput;
            var inputBytes = data.Take(sizeofInput).ToArray();
            Input = inputSerializer.DeserializeInput(inputBytes);

            if (data.Length > sizeofInput)
            {
                AcknowledgedServerTicks = MemoryMarshal.Cast<byte, int>(data[sizeofInput..].AsSpan()).ToArray();
                
                // int ackByteLength = data[sizeofInput..].Length;
                // AcknowledgedServerTicks = new int[ackByteLength / sizeof(int)];
                // Buffer.BlockCopy(data, sizeofInput, AcknowledgedServerTicks, 0, ackByteLength);
            }
            else
            {
                AcknowledgedServerTicks = System.Array.Empty<int>();
            }
        }

        public byte[] ToMessage(InputSerializer inputSerializer)
        {
            List<byte> data = new();
            data.AddRange(inputSerializer.SerializeInput(Input));
            data.AddRange(MemoryMarshal.AsBytes(AcknowledgedServerTicks.AsSpan()).ToArray());
            // data.AddRange(AcknowledgedServerTicks.SelectMany(BitConverter.GetBytes));
            return data.ToArray();
        }
    }
}
