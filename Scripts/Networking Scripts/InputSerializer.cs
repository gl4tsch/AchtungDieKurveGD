namespace ADK.Net
{
    public abstract class InputSerializer
    {
        public abstract int SizeofInput { get; }
        public abstract ISerializableInput DeserializeInput(byte[] inputData);
        public byte[] SerializeInput(ISerializableInput input)
        {
            return input.Serialize();
        }
    }
}
