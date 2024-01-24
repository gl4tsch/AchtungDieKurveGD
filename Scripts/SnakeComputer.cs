using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ADK.Net;
using Godot;
using Godot.Collections;
using Godot.NativeInterop;

namespace ADK
{
    public class SnakeComputer
    {
        RenderingDevice rd;
        Rid arenaTexWrite;
        Arena arena;

        RDShaderFile snakeShaderFile;
        Rid snakeShader;
        Rid snakePipeline;
        Rid snakeUniformSet;
        Rid snakeBuffer;
        Rid collisionBuffer;
        Rid lineBuffer;

        List<Snake> snakes;
        List<Snake> aliveSnakes = new();

        // gradually dequeued and drawn each frame
        Queue<LineData> lineDrawDataBuffer = new();
        uint maxAdditionalLinesPerSnakePerFrame = 3;

        public SnakeComputer(Arena arena, List<Snake> snakes, RenderingDevice rd, RDShaderFile computeShader, Rid arenaTexWrite)
        {
            this.arena = arena;
            this.snakes = snakes;
            this.rd = rd;
            snakeShaderFile = computeShader;
            this.arenaTexWrite = arenaTexWrite;
            // InitTestSnakes(1);
            InitSnakes();
            InitSnakeComputeShader();
        }

        ~SnakeComputer()
        {
            if (rd.TextureIsValid(arenaTexWrite))
            {
                rd.FreeRid(arenaTexWrite);
            }
            if (snakeShader.IsValid)
            {
                rd.FreeRid(snakeShader);
            }
            if (rd.RenderPipelineIsValid(snakePipeline))
            {
                rd.FreeRid(snakePipeline);
            }
            if (rd.UniformSetIsValid(snakeUniformSet))
            {
                rd.FreeRid(snakeUniformSet);
            }
            if (snakeBuffer.IsValid)
            {
                rd.FreeRid(snakeBuffer);
            }
            if (collisionBuffer.IsValid)
            {
                rd.FreeRid(collisionBuffer);
            }
            if (lineBuffer.IsValid)
            {
                rd.FreeRid(lineBuffer);
            }
        }

        void InitTestSnakes(int snakeCount)
        {
            snakes = new();
            for (int i = 0; i < snakeCount; i++)
            {
                snakes[i] = new Snake();
            }
            InitSnakes();
        }

        void InitSnakes()
        {
            //snakes = GameManager.Instance.Snakes;
            foreach (Snake snake in snakes)
            {
                //snake.Spawn(new Vector2I((int)arena.Width, (int)arena.Height));
                aliveSnakes.Add(snake);
                snake.Died += s => aliveSnakes.Remove(s);
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
            snakeBuffer = rd.StorageBufferCreate(sizeof(uint) + LineData.SizeInByte * (uint)snakes.Count);
            // create a snake uniform to assign the snake buffer to the rendering device
            var snakeUniform = new RDUniform
            {
                UniformType = RenderingDevice.UniformType.StorageBuffer,
                Binding = 1
            };
            snakeUniform.AddId(snakeBuffer);

            // collision buffer
            collisionBuffer = rd.StorageBufferCreate(sizeof(int) * (uint)snakes.Count);
            var collisionUniform = new RDUniform
            {
                UniformType = RenderingDevice.UniformType.StorageBuffer,
                Binding = 2
            };
            collisionUniform.AddId(collisionBuffer);

            // line buffer
            lineBuffer = rd.StorageBufferCreate(sizeof(uint) + LineData.SizeInByte * (uint)snakes.Count * maxAdditionalLinesPerSnakePerFrame);
            var lineUniform = new RDUniform{
                UniformType = RenderingDevice.UniformType.StorageBuffer,
                Binding = 3
            };
            lineUniform.AddId(lineBuffer);

            snakeUniformSet = rd.UniformSetCreate(new Array<RDUniform>{ arenaUniform, snakeUniform, collisionUniform, lineUniform }, snakeShader, 0);
        }

        public void Reset()
        {
            aliveSnakes.Clear();
            foreach (Snake snake in snakes)
            {
                snake.Spawn(new Vector2I((int)arena.Width, (int)arena.Height));
                aliveSnakes.Add(snake);
            }
        }

        public void HandleSnakeInput(InputEventKey keyEvent)
        {
            foreach (Snake snake in snakes)
            {
                snake.HandleInput(keyEvent);
            }
        }

        public void HandleSnakeInput(List<SnakeInput> inputs)
        {
            for (int i = 0; i < inputs.Count; i++)
            {
                SnakeInput input = inputs[i];
                snakes[i].HandleInput(input);
            }
        }

        public void UpdateSnakes(double deltaT)
        {
            aliveSnakes.RemoveAll(s => !s.IsAlive);
            // we have a winner
            if (aliveSnakes.Count <= 1)
            {
                GameManager.Instance?.ActiveArenaScene?.EndRound(aliveSnakes.Count == 1 ? aliveSnakes[0] : null);
            }
            // but the last player may still move while the round ends
            if (aliveSnakes.Count == 0)
            {
                return;
            }
            List<LineData> snakesDrawData = GenerateSnakeDrawData(deltaT);
            ComputeSnakesSync(snakesDrawData.ToArray());
            CheckForCollisions();
            HandleSnakeExplosionRequests();
        }

        // this also fills lineDrawBuffer with lines from gaps and abilities and the like
        List<LineData> GenerateSnakeDrawData(double deltaT)
        {
            List<LineData> snakesDrawData = new();
            foreach (var snake in aliveSnakes)
            {
                snake.Update((float)deltaT);
                // snake draw data
                snakesDrawData.Add(snake.GetSnakeDrawData());
                // fill line draw buffer
                foreach (var line in snake.GetLineDrawData())
                {
                    lineDrawDataBuffer.Enqueue(line);
                }
            }
            return snakesDrawData;
        }

        void ComputeSnakesSync(LineData[] snakesData)
        {
            ComputeSnakesAsync(snakesData);
            //rd.Sync();
            rd.Barrier(RenderingDevice.BarrierMask.Compute);
        }

        void ComputeSnakesAsync(LineData[] snakesData)
        {
            uint snakeCount = (uint)snakesData.Length;

            // update snake data buffer and drain line buffer
            List<byte> snakesBytes = new();
            List<LineData> lineDrawData = new();
            foreach (var data in snakesData)
            {
                snakesBytes.AddRange(data.ToByteArray());
                // drain line buffer
                for (int i = 0; i < maxAdditionalLinesPerSnakePerFrame; i++)
                {
                    if (!lineDrawDataBuffer.TryDequeue(out LineData line))
                    {
                        break;
                    }
                    lineDrawData.Add(line);
                }
            }
            // snake count
            rd.BufferUpdate(snakeBuffer, 0, sizeof(uint), BitConverter.GetBytes(snakeCount));
            // snake data
            rd.BufferUpdate(snakeBuffer, sizeof(uint), (uint)snakesBytes.Count, snakesBytes.ToArray());

            // clear collision data buffer
            // apparently this gets initialized to all 0s automatically
            byte[] collisionBytes = new byte[snakes.Count * sizeof(int)];
            rd.BufferUpdate(collisionBuffer, 0, (uint)collisionBytes.Length, collisionBytes);

            // line draw data
            List<byte> lineBytes = new();
            foreach (var line in lineDrawData)
            {
                lineBytes.AddRange(line.ToByteArray());
            }
            // line count
            rd.BufferUpdate(lineBuffer, 0, sizeof(uint), BitConverter.GetBytes((uint)lineDrawData.Count));
            // line data
            rd.BufferUpdate(lineBuffer, sizeof(uint), (uint)lineBytes.Count, lineBytes.ToArray());

            var computeList = rd.ComputeListBegin();
            rd.ComputeListBindComputePipeline(computeList, snakePipeline);
            rd.ComputeListBindUniformSet(computeList, snakeUniformSet, 0);
            rd.ComputeListDispatch(computeList, xGroups: arena.Width / 8, yGroups: arena.Height / 8, zGroups: 1);
            rd.ComputeListEnd();

            // force the GPU to start the commands
            //rd.Submit();
        }

        void CheckForCollisions()
        {
            // get collision output
            byte[] collisionData = rd.BufferGetData(collisionBuffer, 0, (uint)aliveSnakes.Count * sizeof(int));
            int[] collisions = new int[collisionData.Length];
            Buffer.BlockCopy(collisionData, 0, collisions, 0, collisionData.Length);

            List<Snake> collidedSnakes = new();
            for (int i = 0; i < collisions.Length; i++)
            {
                if (collisions[i] != 0)
                {
                    collidedSnakes.Add(aliveSnakes[i]);
                }
            }

            // out of bounds fallback
            foreach (var snake in aliveSnakes.Except(collidedSnakes))
            {
                if (snake.PxPosition.X < 0 || snake.PxPosition.X >= arena.Width || snake.PxPosition.Y < 0 || snake.PxPosition.Y >= arena.Height)
                {
                    collidedSnakes.Add(snake);
                }
            }

            // work on separate list because OnCollision removes snake from aliveSnakes
            foreach (var snake in collidedSnakes)
            {
                snake.OnCollision();
            }            
        }

        void HandleSnakeExplosionRequests()
        {
            List<LineFilter> explosionData = new();
            foreach (var snake in snakes)
            {
                explosionData.AddRange(snake.GetExplosionData());
            }
            foreach (var line in explosionData)
            {
                arena.ExplodePixels(line);
            }
        }
    }
}
