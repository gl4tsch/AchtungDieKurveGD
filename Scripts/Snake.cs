using Godot;
using System.Collections.Generic;
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
        // TODO: make statemachine
        public bool IsAlive { get; private set; } = false;

        List<LineData> injectionDrawBuffer = new();

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

        public void Spawn(Vector2I arenaSize)
        {
            var rng = new RandomNumberGenerator();
            PxPosition = new Vector2(rng.RandfRange(0 + arenaSize.X / 4, arenaSize.X - arenaSize.X / 4), rng.RandfRange(0 + arenaSize.Y / 4, arenaSize.Y - arenaSize.Y / 4));
            Vector2 arenaCenter = arenaSize / 2;
            Direction = (arenaCenter - PxPosition).Normalized();
            TurnSign = 0;
            IsAlive = true;
        }

        public void HandleInput(InputEventKey keyEvent)
        {
            if (!IsAlive)
            {
                return;
            }

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
                Ability?.Activate(this);
            }
        }

        public void Update(float delta)
        {
            if (!IsAlive)
            {
                return;
            }

            Direction = Direction.Rotated(TurnSign * TurnRate * delta);
            pxPrevPos = PxPosition;
            PxPosition = pxPrevPos + Direction * MoveSpeed * delta;
        }

        public void OnCollision()
        {
            GD.Print(Name + " had a collision!");
            IsAlive = false;
        }

        public LineData GetSnakeDrawData()
        {
            return new LineData()
            {
                prevPosX = pxPrevPos.X,
                prevPosY = pxPrevPos.Y,
                newPosX = PxPosition.X,
                newPosY = PxPosition.Y,
                halfThickness = PxThickness / 2f,
                colorR = Color.R,
                colorG = Color.G,
                colorB = Color.B,
                colorA = Color.A,
                clipMode = 0
            };
        }

        /// <summary>
        /// gaps and abilities and the like
        /// </summary>
        /// <returns>draw data no collision checks should be done with</returns>
        public List<LineData> GetLineDrawData()
        {
            List<LineData> data = new();
            data.AddRange(injectionDrawBuffer);
            injectionDrawBuffer.Clear();
            return data;
        }

        public void InjectDrawData(List<LineData> lineDrawData)
        {
            injectionDrawBuffer.AddRange(lineDrawData);
        }
    }

    public struct LineData
    {
        public float prevPosX, prevPosY, newPosX, newPosY;
        public float halfThickness;
        public float colorR, colorG, colorB, colorA;

        /// <summary>
        /// 0 = no clip; 1 = circle around a; 2 = circle around b; 3 = circle around both
        /// </summary>
        public int clipMode;

        //TODO: check for faster ways to do this
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
            writer.Write(clipMode);

            return stream.ToArray();
        }

        public static uint SizeInByte => sizeof(float) * 4 + sizeof(float) + sizeof(float) * 4 + sizeof(int);
    }
}