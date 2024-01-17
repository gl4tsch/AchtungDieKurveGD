namespace ADK.Net
{
    public interface ISerializableInput
    {
        public int SizeInByte { get; }
        public byte[] Serialize();
        public void Deserialize(byte[] data);
    }
}
