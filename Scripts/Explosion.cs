using System.IO;
using Godot;

public class Explosion
{
    public Vector2I center;
    public float radius;
    public float duration = 5; // [s]
    public float elapsedTime = 0;
    public byte[] pixelData;
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

        writer.Write(this.xPos);
        writer.Write(this.yPos);
        writer.Write(this.xDir);
        writer.Write(this.yDir);
        writer.Write(this.r);
        writer.Write(this.g);
        writer.Write(this.b);

        return stream.ToArray();
    }
}