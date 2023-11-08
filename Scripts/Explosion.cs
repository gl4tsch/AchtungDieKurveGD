using System.IO;
using Godot;

namespace ADK
{
    public class Explosion
    {
        public Vector2I center;
        public float radius;
        public float duration = 5; // [s]
        public float elapsedTime = 0;
        public byte[] pixelData;
        public Rid explodyUniformSet;
    }

    public struct ExplodyPixelData
    {
        public float xPos, yPos;
        public float xDir, yDir;
        public float r, g, b;
        public static uint SizeInByte => sizeof(float) * 7;

        public byte[] ToByteArray()
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            writer.Write(xPos);
            writer.Write(yPos);
            writer.Write(xDir);
            writer.Write(yDir);
            writer.Write(r);
            writer.Write(g);
            writer.Write(b);

            return stream.ToArray();
        }
    }
}