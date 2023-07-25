using Godot;
using System;
using System.IO;

public class Snake
{
    public Color Color = new Color(1,0,0,1);
    public float PxThickness = 10f;
    public float MoveSpeed = 100f;
    public float TurnRate = 3f;

    public int TurnSign{get; private set;} = 1; // 0 = straight, -1 = left, 1 = right
    public Vector2 Direction{get; private set;} = Vector2.Right;
    public Vector2 PxPosition{get; private set;} = Vector2.Zero;
    Vector2 pxPrevPos;

    public void RandomizeStartPos(Vector2I arenaSize)
    {
        var rng = new RandomNumberGenerator();
        PxPosition = new Vector2(rng.RandfRange(0 + arenaSize.X / 4, arenaSize.X - arenaSize.X / 4), rng.RandfRange(0 + arenaSize.Y / 4, arenaSize.Y - arenaSize.Y / 4));
        Vector2 arenaCenter = arenaSize / 2;
        Direction = (arenaCenter - PxPosition).Normalized();
    }

    public void Update(float delta)
    {
        Direction = Direction.Rotated(TurnSign * TurnRate * delta);

        pxPrevPos = PxPosition;
        PxPosition = pxPrevPos + Direction * MoveSpeed * delta;
    }

    public SnakeData GetComputeData()
    {
        return new SnakeData()
        {
            prevPosX = Mathf.RoundToInt(pxPrevPos.X),
            prevPosY = Mathf.RoundToInt(pxPrevPos.Y),
            newPosX = Mathf.RoundToInt(PxPosition.X),
            newPosY = Mathf.RoundToInt(PxPosition.Y),
            halfThickness = Mathf.RoundToInt(PxThickness / 2),
            colorR = Color.R,
            colorG = Color.G,
            colorB = Color.B,
            colorA = Color.A,
            collision = 0
        };
    }
}

public struct SnakeData
{
    public int prevPosX, prevPosY, newPosX, newPosY;
    public int halfThickness;
    public float colorR, colorG, colorB, colorA;
    public int collision; // bool

    //TODO: faster
    public byte[] ToByteArray()
    {
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);

        writer.Write(this.prevPosX);
        writer.Write(this.prevPosY);
        writer.Write(this.newPosX);
        writer.Write(this.newPosY);
        writer.Write(this.halfThickness);
        writer.Write(this.colorR);
        writer.Write(this.colorG);
        writer.Write(this.colorB);
        writer.Write(this.colorA);
        writer.Write(this.collision);

        return stream.ToArray();
    }

    public static uint SizeInByte => sizeof(int) * 5 + sizeof(float) * 4 + sizeof(int);
}
