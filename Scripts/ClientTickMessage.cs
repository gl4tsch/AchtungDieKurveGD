using System.Collections.Generic;
using System.Linq;

namespace ADK.Net
{
    /// <summary>
    /// what each client sends to the server every tick
    /// </summary>
    public class ClientTickMessage
    {
        public InputFlags Input;
        public int[] AcknowledgedServerTicks;

        /// <summary>
        /// an empty message
        /// </summary>
        public ClientTickMessage()
        {
            Input = InputFlags.None;
        }

        /// <summary>
        /// decode message
        /// </summary>
        public ClientTickMessage(int[] data)
        {
            Input = (InputFlags)data[0];
            if (data.Length > 1)
            {
                AcknowledgedServerTicks = data[1..];
            }
        }

        public int[] ToMessage()
        {
            List<int> data = new();
            data.Add((int)Input);
            data.AddRange(AcknowledgedServerTicks);
            return data.ToArray();
        }
    }
}
