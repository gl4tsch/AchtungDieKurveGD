using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ADK
{
    public class Snake
    {
        // base stats
        public string Name { get; set; } = "Snake";
        public Color Color { get; private set; } = new Color(1, 0, 0, 1);
        public float PxThickness { get; private set; } = 10f;
        public float MoveSpeed { get; private set; } = 100f;
        public float TurnRate {get; private set; } = 3f;
        public float GapFrequency { get; private set; } = 400;
        public float GapWidthRelToThickness { get; private set; } = 3;
        public float GapWidth => PxThickness * GapWidthRelToThickness;

        // stat modifier
        public float MoveSpeedModifier { get; set; } = 1f;
        public float TurnRateModifier { get; set; } = 1f;
        public float ThicknessModifier { get; set; } = 1f;

        public int TurnSign { get; private set; } = 0; // 0 = straight, -1 = left, 1 = right
        /// <summary>
        /// this is always normalized
        /// </summary>
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
        public event Action<Snake> Died;

        List<LineData> injectionDrawBuffer = new();
        List<LineFilter> explosionBuffer = new();

        // gap
        // turnSign is used to combine segments if possible
        Stack<(int turnSign, LineData segment)> gapSegmentBuffer = new();
        float distSinceLastGap = 0;

        public Snake()
        {
            RandomizeColor();
        }

        public Snake(string name) : this()
        {
            Name = name;
        }

        public Snake(string name, SnakeSettings settings) : this(name)
        {
            ApplySettings(settings);
        }

        public void ApplySettings(SnakeSettings settings)
        {
            PxThickness = settings.Thickness;
            MoveSpeed = settings.MoveSpeed;
            TurnRate = settings.TurnRate;
            GapFrequency = settings.GapFrequency;
            GapWidthRelToThickness = settings.GapWidthRelToThickness;
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
            
            // poll input for turn sign initialization
            bool left = Input.IsKeyPressed(TurnLeftKey);
            bool right = Input.IsKeyPressed(TurnRightKey);
            TurnSign = left && !right ? -1 : right && !left ? 1 : 0;

            // reset modifiers
            MoveSpeedModifier = 1f;
            TurnRateModifier = 1f;
            ThicknessModifier = 1f;

            // reset gap
            distSinceLastGap = 0;
            gapSegmentBuffer.Clear();

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

        public void Update(float deltaT)
        {
            if (!IsAlive)
            {
                return;
            }
            
            Direction = Direction.Rotated(TurnSign * TurnRate * TurnRateModifier * deltaT);
            pxPrevPos = PxPosition;
            PxPosition = pxPrevPos + Direction * MoveSpeed * MoveSpeedModifier * deltaT;

            UpdateGap();
            Ability?.Tick(deltaT);
        }

        void UpdateGap()
        {
            distSinceLastGap += (PxPosition - pxPrevPos).Length();

            // add to gap buffer
            if (distSinceLastGap > GapFrequency)
            {
                LineData gapSegment = new()
                {
                    prevPosX = pxPrevPos.X,
                    prevPosY = pxPrevPos.Y,
                    newPosX = PxPosition.X,
                    newPosY = PxPosition.Y,
                    halfThickness = PxThickness * ThicknessModifier / 2,
                    colorR = 0,
                    colorG = 0,
                    colorB = 0,
                    colorA = 0,
                    clipMode = 1
                };

                // check if data can be combined
                if (gapSegmentBuffer.Count > 0)
                {
                    var lastSegment = gapSegmentBuffer.Peek();
                    // can only combine if going straight for now
                    if (lastSegment.turnSign == 0 && TurnSign == 0)
                    {
                        // discard old segment
                        gapSegmentBuffer.Pop();
                        // add to current segment
                        gapSegment.prevPosX = lastSegment.segment.prevPosX;
                        gapSegment.prevPosY = lastSegment.segment.prevPosY;
                    }
                }

                // add to buffer
                gapSegmentBuffer.Push((TurnSign, gapSegment));
            }

            // gap end
            if (distSinceLastGap > GapFrequency + GapWidth)
            {
                // clip end of gap
                var lastSegment = gapSegmentBuffer.Pop();
                lastSegment.segment.clipMode = 3;
                gapSegmentBuffer.Push(lastSegment);

                List<LineData> gapData = gapSegmentBuffer.Select(gs => gs.segment).ToList();
                InjectDrawData(gapData);

                gapSegmentBuffer.Clear();
                distSinceLastGap -= GapFrequency + GapWidth;
            }
        }

        public void OnCollision()
        {
            GD.Print(Name + " had a collision!");
            RequestExplosion(new LineFilter()
            {
                startPosX = PxPosition.X,
                startPosY = PxPosition.Y,
                endPosX = PxPosition.X,
                endPosY = PxPosition.Y,
                halfThickness = PxThickness * ThicknessModifier,
                clipMode = 0
            });
            AudioManager.Instance?.PlaySound(SFX.SnakeDeathExplosion);
            Kill();
        }

        public void Kill()
        {
            IsAlive = false;
            Died?.Invoke(this);
        }

        public LineData GetSnakeDrawData()
        {
            return new LineData()
            {
                prevPosX = pxPrevPos.X,
                prevPosY = pxPrevPos.Y,
                newPosX = PxPosition.X,
                newPosY = PxPosition.Y,
                halfThickness = PxThickness * ThicknessModifier / 2f,
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

        public List<LineFilter> GetExplosionData()
        {
            List<LineFilter> data = new();
            data.AddRange(explosionBuffer);
            explosionBuffer.Clear();
            return data;
        }

        public void InjectDrawData(List<LineData> lineDrawData)
        {
            injectionDrawBuffer.AddRange(lineDrawData);
        }

        public void RequestExplosion(LineFilter pixels)
        {
            explosionBuffer.Add(pixels);
        }

        public void Teleport(Vector2 position)
        {
            pxPrevPos = position;
            PxPosition = position;
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
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

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