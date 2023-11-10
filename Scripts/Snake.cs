using Godot;
using System.IO;

namespace ADK
{
    public class Snake
    {
        public string Name { get; set; } = "Snake";
        public Color Color { get; private set; } = new Color(1, 0, 0, 1);
        public float PxThickness = 10f;
        public float MoveSpeed = 100f;
        public float TurnRate = 3f;

        public int TurnSign { get; private set; } = 0; // 0 = straight, -1 = left, 1 = right
        public Vector2 Direction { get; private set; } = Vector2.Right;
        public Vector2 PxPosition { get; private set; } = Vector2.Zero;
        Vector2 pxPrevPos;

        // Input
        // possible alternative: InputAction
        public Key TurnLeftKey = Key.A;
        public Key TurnRightKey = Key.D;
        public Key FireKey = Key.W;

        public Ability Ability { get; set; }

        public Snake()
        {
            RandomizeColor();
        }

        public Snake(string name) : this()
        {
            Name = name;
        }

        public void RandomizeColor()
        {
            var rng = new RandomNumberGenerator();
            Color = Color.FromHsv(rng.Randf(), 1, 1);
        }

        public void RandomizeStartPos(Vector2I arenaSize)
        {
            var rng = new RandomNumberGenerator();
            PxPosition = new Vector2(rng.RandfRange(0 + arenaSize.X / 4, arenaSize.X - arenaSize.X / 4), rng.RandfRange(0 + arenaSize.Y / 4, arenaSize.Y - arenaSize.Y / 4));
            Vector2 arenaCenter = arenaSize / 2;
            Direction = (arenaCenter - PxPosition).Normalized();
        }

        public void HandleInput(InputEventKey keyEvent)
        {
            // turn left
            if (keyEvent.Keycode == TurnLeftKey)
            {
                TurnSign += keyEvent.IsPressed() ? -1 : 1;
            }
            // turn right
            if (keyEvent.Keycode == TurnRightKey)
            {
                TurnSign += keyEvent.IsPressed() ? 1 : -1;
            }
            // fire
            if (keyEvent.Keycode == FireKey && keyEvent.IsPressed())
            {
                GD.Print("Fire!");
            }
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

            writer.Write(prevPosX);
            writer.Write(prevPosY);
            writer.Write(newPosX);
            writer.Write(newPosY);
            writer.Write(halfThickness);
            writer.Write(colorR);
            writer.Write(colorG);
            writer.Write(colorB);
            writer.Write(colorA);
            writer.Write(collision);

            return stream.ToArray();
        }

        public static uint SizeInByte => sizeof(int) * 5 + sizeof(float) * 4 + sizeof(int);
    }
}