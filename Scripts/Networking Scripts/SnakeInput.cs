using System;

namespace ADK.Net
{
    public class SnakeInput : ISerializableInput
    {
        InputFlags input;
        public int SizeInByte => 1;
        public static int SizeofInput => 1;

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
            // data should only consist of one byte
            input = (InputFlags)data[0];
        }

        public byte[] Serialize()
        {
            return new byte[]{(byte)input};
        }

        public override string ToString()
        {
            return input.ToString();
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
