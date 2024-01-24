using System;

namespace ADK.Net
{
    public class SnakeInput : ISerializableInput
    {
        public InputFlags Input;
        public int SizeInByte => 1;
        public static int SizeofInput => 1;

        public SnakeInput()
        {
            Input = InputFlags.None;
        }

        public SnakeInput(InputFlags input)
        {
            this.Input = input;
        }

        public SnakeInput(byte[] data)
        {
            Deserialize(data);
        }

        public void Deserialize(byte[] data)
        {
            // data should only consist of one byte
            Input = (InputFlags)data[0];
        }

        public byte[] Serialize()
        {
            return new byte[]{(byte)Input};
        }

        public override string ToString()
        {
            return Input.ToString();
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
