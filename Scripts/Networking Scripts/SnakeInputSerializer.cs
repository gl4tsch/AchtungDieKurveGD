using System;

namespace ADK.Net
{
    public class SnakeInputSerializer : InputSerializer
    {
        public override int SizeofInput => SnakeInput.SizeofInput;

        public override ISerializableInput DeserializeInput(byte[] inputData)
        {
            return new SnakeInput(inputData);
        }
    }
}