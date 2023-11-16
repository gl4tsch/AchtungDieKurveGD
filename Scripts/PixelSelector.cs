using System.Net;
using System;
using Godot;
using Godot.Collections;
using System.IO;
using System.Security.Cryptography;

namespace ADK
{
    public class PixelSelector
    {
        RenderingDevice rd;
        Rid arenaTexRead, arenaTexWrite;
        uint pxWidth, pxHeight;

        RDShaderFile pixelSelectShaderFile;
        Rid pixelSelectShader;
        Rid pixelSelectPipeline;
        Rid pixelSelectUniformSet;
        Rid pxFilterBuffer, selectedPixelsBuffer;

        uint maxPixelsPerSelection = 512 * 512;

        public PixelSelector(RenderingDevice rd, RDShaderFile computeShader, Rid arenaTextureRead, Rid arenaTextureWrite, uint pxWidth, uint pxHeight)
        {
            this.rd = rd;
            pixelSelectShaderFile = computeShader;
            arenaTexRead = arenaTextureRead;
            arenaTexWrite = arenaTextureWrite;
            this.pxWidth = pxWidth;
            this.pxHeight = pxHeight;
            InitPixelSelectComputeShader();
        }

        void InitPixelSelectComputeShader()
        {
            // load GLSL shader
            // pixelSelectShaderFile = GD.Load<RDShaderFile>("res://Scripts/PixelSelectCompute.glsl");
            var pxSelectBytecode = pixelSelectShaderFile.GetSpirV();
            pixelSelectShader = rd.ShaderCreateFromSpirV(pxSelectBytecode);

            // Create a compute pipeline
            pixelSelectPipeline = rd.ComputePipelineCreate(pixelSelectShader);

            // arena input tex uniform
            var arenaUniform = new RDUniform
            {
                UniformType = RenderingDevice.UniformType.Image,
                Binding = 0 // the in tex
            };
            arenaUniform.AddId(arenaTexWrite);

            // create filter buffer
            pxFilterBuffer = rd.StorageBufferCreate(LineFilter.SizeInByte);

            // create a uniform to assign the buffer to the rendering device
            var pxFilterUniform = new RDUniform
            {
                UniformType = RenderingDevice.UniformType.StorageBuffer,
                Binding = 1
            };
            pxFilterUniform.AddId(pxFilterBuffer);

            // create output buffer
            selectedPixelsBuffer = rd.StorageBufferCreate(Pixel.SizeInByte * maxPixelsPerSelection);

            // create a uniform to assign the buffer to the rendering device
            var selectedPxUniform = new RDUniform
            {
                UniformType = RenderingDevice.UniformType.StorageBuffer,
                Binding = 2
            };
            selectedPxUniform.AddId(selectedPixelsBuffer);

            pixelSelectUniformSet = rd.UniformSetCreate(new Array<RDUniform> { arenaUniform, pxFilterUniform, selectedPxUniform }, pixelSelectShader, 0);
        }

        public Pixel[] SelectPixels(Vector2I center, float radius)
        {
            return SelectPixels(center, center, radius);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="startPos"></param>
        /// <param name="endPos"></param>
        /// <param name="halfThickness"></param>
        /// <param name="clipMode">0 = no clipping; 1 = circle around start; 2 = circlie around end; 3 = circle around both;</param>
        /// <returns></returns>
        public Pixel[] SelectPixels(Vector2 startPos, Vector2 endPos, float halfThickness, int clipMode = 0)
        {
            // select the pixels involved in the explosion
            LineFilter filter = new()
            {
                startPosX = startPos.X,
                startPosY = startPos.Y,
                endPosX = endPos.X,
                endPosY = endPos.Y,
                halfThickness = halfThickness,
                clipMode = clipMode
            };

            rd.BufferUpdate(pxFilterBuffer, 0, LineFilter.SizeInByte, filter.ToByteArray());
            // reset the array insertion index
            rd.BufferUpdate(selectedPixelsBuffer, 0, sizeof(uint), BitConverter.GetBytes((uint)0));

            var computeList = rd.ComputeListBegin();
            rd.ComputeListBindComputePipeline(computeList, pixelSelectPipeline);
            rd.ComputeListBindUniformSet(computeList, pixelSelectUniformSet, 0);
            rd.ComputeListDispatch(computeList, xGroups: pxWidth / 8, yGroups: pxHeight / 8, zGroups: 1);
            rd.ComputeListEnd();

            // force the GPU to start the commands
            rd.Submit();
            rd.Sync();

            byte[] byteSize = rd.BufferGetData(selectedPixelsBuffer, 0, sizeof(uint));
            uint pixelCount = BitConverter.ToUInt32(byteSize);
            GD.Print("# exploding pixels: " + pixelCount);

            // offset insertion index and one data entry (because atomic add off by one in compute shader)
            var pixelData = rd.BufferGetData(selectedPixelsBuffer, sizeof(uint), Pixel.SizeInByte * pixelCount);
            Pixel[] pixels = new Pixel[pixelCount];

            using (MemoryStream pxStream = new(pixelData))
            using (BinaryReader pxReader = new BinaryReader(pxStream))
            {
                for (int i = 0; i < pixelCount; i++)
                {
                    byte[] onePixel = pxReader.ReadBytes((int)Pixel.SizeInByte);
                    pixels[i] = new Pixel(onePixel);
                }
            }
            return pixels;
        }
    }

    public struct Pixel
    {
        public uint posX, posY;
        public float r, g, b;
        public static uint SizeInByte => sizeof(uint) * 2 + sizeof(float) * 3;

        public Pixel(byte[] pixelData)
        {
            using (MemoryStream pxStream = new(pixelData))
            using (BinaryReader pxReader = new BinaryReader(pxStream))
            {
                byte[] posXBytes = pxReader.ReadBytes(sizeof(uint));
                posX = BitConverter.ToUInt32(posXBytes);
                byte[] posYBytes = pxReader.ReadBytes(sizeof(uint));
                posY = BitConverter.ToUInt32(posYBytes);
                byte[] rBytes = pxReader.ReadBytes(sizeof(float));
                r = BitConverter.ToSingle(rBytes);
                byte[] gBytes = pxReader.ReadBytes(sizeof(float));
                g = BitConverter.ToSingle(gBytes);
                byte[] bBytes = pxReader.ReadBytes(sizeof(float));
                b = BitConverter.ToSingle(bBytes);
            }
        }
    }

    public struct LineFilter
    {
        public float startPosX, startPosY, endPosX, endPosY;
        public float halfThickness;
        /// <summary>
        /// 0 = no clip; 1 = circle around a; 2 = circle around b; 3 = circle around both
        /// </summary>
        public int clipMode;

        public static uint SizeInByte => sizeof(float) * 5 + sizeof(int);

        public byte[] ToByteArray()
        {
            using MemoryStream stream = new();
            using BinaryWriter writer = new(stream);

            writer.Write(startPosX);
            writer.Write(startPosY);
            writer.Write(endPosX);
            writer.Write(endPosY);
            writer.Write(halfThickness);
            writer.Write(clipMode);

            return stream.ToArray();
        }
    }
}