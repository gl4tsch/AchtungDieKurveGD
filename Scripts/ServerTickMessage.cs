using System.Collections.Generic;
using System.Linq;

namespace ADK.Net
{
    /// <summary>
    /// what the server sends to each client every tick
    /// </summary>
    public class ServerTickMessage
    {
        public int TickNumber;
        public InputFlags[] ClientInputs; //TODO: send all input not acknowledged by the client yet

        public ServerTickMessage(){}
        public ServerTickMessage(int[] tickData)
        {
            TickNumber = tickData[0];
            ClientInputs = tickData[1..].Cast<InputFlags>().ToArray();
        }

        public int[] ToMessage()
        {
            List<int> data = new();
            data.Add(TickNumber);
            data.AddRange(ClientInputs.Cast<int>());
            return data.ToArray();
        }
    }
}
