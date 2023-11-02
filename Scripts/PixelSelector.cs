using System;
using Godot;
using Godot.Collections;

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

    public PixelSelector(RenderingDevice rd, Rid arenaTextureRead, Rid arenaTextureWrite, uint pxWidth, uint pxHeight)
    {
        this.rd = rd;
        this.arenaTexRead = arenaTextureRead;
        this.arenaTexWrite = arenaTextureWrite;
        this.pxWidth = pxWidth;
        this.pxHeight = pxHeight;
        InitPixelSelectComputeShader();
    }

    void InitPixelSelectComputeShader()
    {
        // load GLSL shader
        pixelSelectShaderFile = GD.Load<RDShaderFile>("res://Scripts/PixelSelectCompute.glsl");
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
        pxFilterBuffer = rd.StorageBufferCreate(sizeof(int) * 2 + sizeof(float));

        // create a uniform to assign the buffer to the rendering device
        var pxFilterUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 1
        };
        pxFilterUniform.AddId(pxFilterBuffer);

        // create output buffer
        selectedPixelsBuffer = rd.StorageBufferCreate(sizeof(uint) + sizeof(int) * 2 * maxPixelsPerSelection);

        // create a uniform to assign the buffer to the rendering device
        var selectedPxUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 2
        };
        selectedPxUniform.AddId(selectedPixelsBuffer);

        pixelSelectUniformSet = rd.UniformSetCreate(new Array<RDUniform> { arenaUniform, pxFilterUniform, selectedPxUniform }, pixelSelectShader, 0);
    }

    public int[] SelectPixels(Vector2I center, float radius)
    {
        // select the pixels involved in the explosion
        var xBytes = BitConverter.GetBytes(center.X);
        var yBytes = BitConverter.GetBytes(center.Y);
        var rBytes = BitConverter.GetBytes(radius);
        var data = new byte[xBytes.Length + yBytes.Length + rBytes.Length];
        xBytes.CopyTo(data, 0);
        yBytes.CopyTo(data, xBytes.Length);
        rBytes.CopyTo(data, xBytes.Length + yBytes.Length);

        rd.BufferUpdate(pxFilterBuffer, 0, sizeof(int) * 2 + sizeof(float), data);
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
        uint numCoords = BitConverter.ToUInt32(byteSize);
        GD.Print("# exploding pixels: " + numCoords / 2);

        // offset by insertion index size
        var pixelData = rd.BufferGetData(selectedPixelsBuffer, sizeof(uint), sizeof(int) * numCoords);
        int[] pixels = new int[numCoords];
        Buffer.BlockCopy(pixelData, 0, pixels, 0, pixelData.Length);
        return pixels;
    }
}