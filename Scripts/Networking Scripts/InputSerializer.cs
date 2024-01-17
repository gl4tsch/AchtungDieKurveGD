namespace ADK.Net
{
    public abstract class InputSerializer
    {
        public abstract int SizeofInput { get; }
        public abstract byte[] SerializeInput(ISerializableInput input);
        public abstract ISerializableInput DeserializeInput(byte[] inputData);
    }
}
