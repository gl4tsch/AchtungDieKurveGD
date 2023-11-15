using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Godot;
using Godot.Collections;
using Godot.NativeInterop;

namespace ADK
{
    public class SnakeComputer
    {
        RenderingDevice rd;
        Rid arenaTexRead, arenaTexWrite;
        Arena arena;

        RDShaderFile snakeShaderFile;
        Rid snakeShader;
        Rid snakePipeline;
        Rid snakeUniformSet;
        Rid snakeBuffer;
        Rid collisionBuffer;

        Snake[] snakes;
        List<Snake> aliveSnakes = new();
        List<SnakeData> snakesData = new();

        public SnakeComputer(Arena arena, RenderingDevice rd, RDShaderFile computeShader, Rid arenaTexRead, Rid arenaTexWrite)
        {
            this.arena = arena;
            this.rd = rd;
            snakeShaderFile = computeShader;
            this.arenaTexRead = arenaTexRead;
            this.arenaTexWrite = arenaTexWrite;
            //InitTestSnakes(1);
            InitSnakes();
            InitSnakeComputeShader();
        }

        void InitTestSnakes(int snakeCount)
        {
            snakes = new Snake[snakeCount];
            for (int i = 0; i < snakeCount; i++)
            {
                snakes[i] = new Snake();
            }
            InitSnakes();
        }

        void InitSnakes()
        {
            snakes = GameManager.Instance.Snakes.ToArray();
            foreach (Snake snake in snakes)
            {
                snake.Spawn(new Vector2I((int)arena.Width, (int)arena.Height));
                aliveSnakes.Add(snake);
            }
        }

        void InitSnakeComputeShader()
        {
            // load snake GLSL shader
            // snakeShaderFile = GD.Load<RDShaderFile>("res://Scripts/SnakeCompute.glsl");
            var snakeBytecode = snakeShaderFile.GetSpirV();
            snakeShader = rd.ShaderCreateFromSpirV(snakeBytecode);

            // Create a compute pipelines
            snakePipeline = rd.ComputePipelineCreate(snakeShader);

            // arena input tex uniform
            var arenaUniform = new RDUniform
            {
                UniformType = RenderingDevice.UniformType.Image,
                Binding = 0 // the in tex
            };
            arenaUniform.AddId(arenaTexWrite);

            // create snake buffer
            snakeBuffer = rd.StorageBufferCreate(SnakeData.SizeInByte * (uint)snakes.Length);
            // create a snake uniform to assign the snake buffer to the rendering device
            var snakeUniform = new RDUniform
            {
                UniformType = RenderingDevice.UniformType.StorageBuffer,
                Binding = 1
            };
            snakeUniform.AddId(snakeBuffer);

            // collision buffer
            collisionBuffer = rd.StorageBufferCreate(sizeof(int) * (uint)snakes.Length);
            var collisionUniform = new RDUniform
            {
                UniformType = RenderingDevice.UniformType.StorageBuffer,
                Binding = 2
            };
            collisionUniform.AddId(collisionBuffer);

            snakeUniformSet = rd.UniformSetCreate(new Array<RDUniform>{ arenaUniform, snakeUniform, collisionUniform }, snakeShader, 0);
        }

        public void HandleSnakeInput(InputEventKey keyEvent)
        {
            foreach (Snake snake in snakes)
            {
                snake.HandleInput(keyEvent);
            }
        }

        public void UpdateSnakes(double deltaT)
        {
            aliveSnakes.RemoveAll(s => !s.IsAlive);
            if (aliveSnakes.Count == 0)
            {
                return;
            }
            UpdateSnakeData(deltaT);
            ComputeSnakesSync(snakesData.ToArray());
            CheckForCollisions();
        }

        void UpdateSnakeData(double deltaT)
        {
            snakesData.Clear();
            foreach (var aliveSnake in aliveSnakes)
            {
                aliveSnake.Update((float)deltaT);
                snakesData.Add(aliveSnake.GetComputeData());
            }
        }

        void ComputeSnakesSync(SnakeData[] snakesData)
        {
            ComputeSnakesAsync(snakesData);
            rd.Sync();
        }

        void ComputeSnakesAsync(SnakeData[] snakesData)
        {
            uint snakeCount = (uint)snakesData.Length;

            // update snake data buffer
            List<byte> snakesBytes = new List<byte>();
            foreach (var data in snakesData)
            {
                snakesBytes.AddRange(data.ToByteArray());
            }
            rd.BufferUpdate(snakeBuffer, 0, (uint)snakesBytes.Count, snakesBytes.ToArray());

            // clear collision data buffer
            byte[] collisionBytes = new byte[snakeCount * sizeof(int)];
            for (int i = 0; i < collisionBytes.Length; i++)
            {
                collisionBytes[i] = 0;
            }
            rd.BufferUpdate(collisionBuffer, 0, (uint)collisionBytes.Length, collisionBytes);

            var computeList = rd.ComputeListBegin();
            rd.ComputeListBindComputePipeline(computeList, snakePipeline);
            rd.ComputeListBindUniformSet(computeList, snakeUniformSet, 0);
            rd.ComputeListDispatch(computeList, xGroups: arena.Width / 8, yGroups: arena.Height / 8, zGroups: 1);
            rd.ComputeListEnd();

            // force the GPU to start the commands
            rd.Submit();
        }

        void CheckForCollisions()
        {
            // get collision output
            byte[] collisionData = rd.BufferGetData(collisionBuffer, 0, (uint)snakesData.Count * sizeof(int));
            int[] collisions = new int[collisionData.Length];
            Buffer.BlockCopy(collisionData, 0, collisions, 0, collisionData.Length);
            for (int i = 0; i < collisions.Length; i++)
            {
                if (collisions[i] != 0)
                {
                    Snake snake = aliveSnakes[i];
                    snake.OnCollision();
                    // maybe explode later to avoid draw race condition?
                    // did not work in quick test
                    arena.ExplodePixels((Vector2I)snake.PxPosition, Mathf.CeilToInt(snake.PxThickness));
                }
            }
        }
    }
}
