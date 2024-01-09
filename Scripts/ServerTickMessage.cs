using System.Collections.Generic;
using System.Linq;

namespace ADK.Net
{
    /// <summary>
    /// what the server sends to each client every tick
    /// </summary>
    public class ServerTickMessage
    {
        // send all input not acknowledged by the client yet
        public SortedList<int, InputFlags[]> ClientInputs = new();

        public ServerTickMessage(){}

        /// <param name="tickData">of the form [tickNumber,[InputFlagsForEachPlayer],...]</param>
        public ServerTickMessage(int[] tickData, int numPlayers)
        {
            int blockLength = numPlayers + 1;
            for (int i = 0; i < tickData.Length; i += blockLength)
            {
                InputFlags[] inputs = tickData[(i+1)..(i+blockLength-1)].Cast<InputFlags>().ToArray();
                ClientInputs.Add(tickData[i], inputs);
            }
        }

        public int[] ToMessage()
        {
            List<int> data = new();
            foreach (var input in ClientInputs)
            {
                data.Add(input.Key);
                data.AddRange(input.Value.Cast<int>());
            }
            return data.ToArray();
        }
    }
}
