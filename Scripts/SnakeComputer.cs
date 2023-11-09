using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

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

        Snake[] snakes;
        SnakeData[] snakesData;

        public SnakeComputer(Arena arena, RenderingDevice rd, RDShaderFile computeShader, Rid arenaTexRead, Rid arenaTexWrite)
        {
            this.arena = arena;
            this.rd = rd;
            snakeShaderFile = computeShader;
            this.arenaTexRead = arenaTexRead;
            this.arenaTexWrite = arenaTexWrite;
            InitTestSnakes(1);
            InitSnakeComputeShader();
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

            snakeUniformSet = rd.UniformSetCreate(new Array<RDUniform> { arenaUniform, snakeUniform }, snakeShader, 0);
        }

        void InitTestSnakes(int snakeCount)
        {
            var rng = new RandomNumberGenerator();
            snakes = new Snake[snakeCount];
            for (int i = 0; i < snakeCount; i++)
            {
                snakes[i] = new Snake
                {
                    Arena = arena
                };
                snakes[i].RandomizeStartPos(new Vector2I((int)arena.Width, (int)arena.Height));
            }
            snakesData = new SnakeData[snakes.Length];
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
            UpdateSnakeData(deltaT);
            ComputeSnakesSync(snakesData);
        }

        void UpdateSnakeData(double deltaT)
        {
            for (int i = 0; i < snakes.Length; i++)
            {
                snakes[i].Update((float)deltaT);
                snakesData[i] = snakes[i].GetComputeData();
            }
        }

        void ComputeSnakesSync(SnakeData[] snakesData)
        {
            ComputeSnakesAsync(snakesData);
            rd.Sync();
        }

        void ComputeSnakesAsync(SnakeData[] snakesData)
        {
            // update snake data buffer
            List<byte> snakesBytes = new List<byte>();

            foreach (var data in snakesData)
            {
                snakesBytes.AddRange(data.ToByteArray());
            }
            rd.BufferUpdate(snakeBuffer, 0, (uint)snakesBytes.Count(), snakesBytes.ToArray());

            var computeList = rd.ComputeListBegin();
            rd.ComputeListBindComputePipeline(computeList, snakePipeline);
            rd.ComputeListBindUniformSet(computeList, snakeUniformSet, 0);
            rd.ComputeListDispatch(computeList, xGroups: arena.Width / 8, yGroups: arena.Height / 8, zGroups: 1);
            rd.ComputeListEnd();

            // force the GPU to start the commands
            rd.Submit();
        }
    }
}
