using System;

namespace ADK.Net
{
    public class SnakeInputSerializer : InputSerializer
    {
        public override int SizeofInput => sizeof(int);

        public override ISerializableInput DeserializeInput(byte[] inputData)
        {
            return new SnakeInput(inputData);
        }
    }
}