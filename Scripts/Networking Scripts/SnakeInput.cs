using System;

namespace ADK.Net
{
    public class SnakeInput : ISerializableInput
    {
        InputFlags input;
        public int SizeInByte => sizeof(int);

        public SnakeInput()
        {
            input = InputFlags.None;
        }

        public SnakeInput(InputFlags input)
        {
            this.input = input;
        }

        public SnakeInput(byte[] data)
        {
            Deserialize(data);
        }

        public void Deserialize(byte[] data)
        {
            input = (InputFlags)BitConverter.ToInt32(data);
        }

        public byte[] Serialize()
        {
            return BitConverter.GetBytes((int)input);
        }
    }

    [Flags]
    public enum InputFlags
    {
        None = 0,
        Left = 1 << 0,
        Right = 1 << 1,
        Fire = 1 << 2
    }
}
