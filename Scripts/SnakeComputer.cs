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

        RDShaderFile snakeShaderFile;
        Rid snakeShader;
        Rid snakePipeline;
        Rid snakeUniformSet;
        Rid snakeBuffer;
        Rid collisionBuffer;
        Rid lineBuffer;

        uint arenaWidth, arenaHeight;
        int snakeCount;

        // gradually dequeued and drawn each frame
        Queue<LineData> lineDrawDataBuffer = new();
        uint maxAdditionalLinesPerFrame = 6;

        public SnakeComputer(uint arenaWidth, uint arenaHeight, int snakeCount, RenderingDevice rd, RDShaderFile computeShader, Rid arenaTexWrite)
        {
            this.arenaWidth = arenaWidth;
            this.arenaHeight = arenaHeight;
            this.snakeCount = snakeCount;
            this.rd = rd;
            snakeShaderFile = computeShader;
            this.arenaTexWrite = arenaTexWrite;
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
            snakeBuffer = rd.StorageBufferCreate(sizeof(uint) + LineData.SizeInByte * (uint)snakeCount);
            // create a snake uniform to assign the snake buffer to the rendering device
            var snakeUniform = new RDUniform
            {
                UniformType = RenderingDevice.UniformType.StorageBuffer,
                Binding = 1
            };
            snakeUniform.AddId(snakeBuffer);

            // collision buffer
            collisionBuffer = rd.StorageBufferCreate(sizeof(int) * (uint)snakeCount);
            var collisionUniform = new RDUniform
            {
                UniformType = RenderingDevice.UniformType.StorageBuffer,
                Binding = 2
            };
            collisionUniform.AddId(collisionBuffer);

            // line buffer
            lineBuffer = rd.StorageBufferCreate(sizeof(uint) + LineData.SizeInByte * maxAdditionalLinesPerFrame);
            var lineUniform = new RDUniform{
                UniformType = RenderingDevice.UniformType.StorageBuffer,
                Binding = 3
            };
            lineUniform.AddId(lineBuffer);

            snakeUniformSet = rd.UniformSetCreate(new Array<RDUniform>{ arenaUniform, snakeUniform, collisionUniform, lineUniform }, snakeShader, 0);
        }

        /// <summary>
        /// this needs to tick regularly to empty the lineDrawDataBuffer
        /// </summary>
        public void Draw(List<LineData> snakesData, List<LineData> lineData)
        {
            // fill line buffer
            foreach (var line in lineData)
            {
                lineDrawDataBuffer.Enqueue(line);
            }

            // drain line buffer
            List<LineData> drainedLineDrawData = new();
            for (int i = 0; i < maxAdditionalLinesPerFrame; i++)
            {
                if (!lineDrawDataBuffer.TryDequeue(out LineData line))
                {
                    break;
                }
                drainedLineDrawData.Add(line);
            }

            ComputeDrawSync(snakesData.ToArray(), drainedLineDrawData);
        }

        void ComputeDrawSync(LineData[] snakesData, List<LineData> lineDrawData)
        {
            ComputeDrawAsync(snakesData, lineDrawData);
            //rd.Sync();
            rd.Barrier(RenderingDevice.BarrierMask.Compute);
        }

        void ComputeDrawAsync(LineData[] snakesData, List<LineData> lineDrawData)
        {
            uint snakeCount = (uint)snakesData.Length;

            // snake draw data
            List<byte> snakesBytes = new();
            foreach (var data in snakesData)
            {
                snakesBytes.AddRange(data.ToByteArray());
            }
            // line draw data
            List<byte> lineBytes = new();
            foreach (var line in lineDrawData)
            {
                lineBytes.AddRange(line.ToByteArray());
            }

            // snake count
            rd.BufferUpdate(snakeBuffer, 0, sizeof(uint), BitConverter.GetBytes(snakeCount));
            // snake data
            rd.BufferUpdate(snakeBuffer, sizeof(uint), (uint)snakesBytes.Count, snakesBytes.ToArray());

            // line count
            rd.BufferUpdate(lineBuffer, 0, sizeof(uint), BitConverter.GetBytes((uint)lineDrawData.Count));
            // line data
            rd.BufferUpdate(lineBuffer, sizeof(uint), (uint)lineBytes.Count, lineBytes.ToArray());

            // clear collision data buffer
            // apparently this gets initialized to all 0s automatically
            byte[] collisionBytes = new byte[snakeCount * sizeof(int)];
            rd.BufferUpdate(collisionBuffer, 0, (uint)collisionBytes.Length, collisionBytes);

            var computeList = rd.ComputeListBegin();
            rd.ComputeListBindComputePipeline(computeList, snakePipeline);
            rd.ComputeListBindUniformSet(computeList, snakeUniformSet, 0);
            rd.ComputeListDispatch(computeList, xGroups: arenaWidth / 8, yGroups: arenaHeight / 8, zGroups: 1);
            rd.ComputeListEnd();

            // force the GPU to start the commands
            //rd.Submit();
        }

        /// <returns>the collision flags for all snakes updated this frame</returns>
        public int[] GetCollisions()
        {
            // get collision output
            byte[] collisionData = rd.BufferGetData(collisionBuffer, 0, (uint)snakeCount * sizeof(int));
            int[] collisions = new int[collisionData.Length];
            Buffer.BlockCopy(collisionData, 0, collisions, 0, collisionData.Length);
            return collisions;
        }
    }
}
