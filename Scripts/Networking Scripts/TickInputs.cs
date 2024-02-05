using System.Collections.Generic;

namespace ADK.Net
{
    public struct TickInputs
    {
        public int TickNumber;
        public SortedList<long, ISerializableInput> PlayersInput;

        public TickInputs(int tickNumber, SortedList<long, ISerializableInput> playersInput)
        {
            TickNumber = tickNumber;
            PlayersInput = playersInput;
        }
    }
}
