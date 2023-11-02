using System.Linq.Expressions;
using System.Linq;
using System.IO;
using Godot;
using Godot.Collections;
using System;
using System.Collections.Generic;

public partial class SnakeComputer : TextureRect
{
    [Export]
    uint pxWidth = 512, pxHeight = 512;

    RenderingDevice rd;
    RDShaderFile snakeShaderFile;
    Rid snakeShader;
    Rid snakePipeline;
    Rid arenaTexRead, arenaTexWrite;
    Rid snakeUniformSet;
    Rid snakeBuffer;

    ExplodeComputer explodeComputer;
    PixelSelector pixelSelector;

    double tempTime = 0;
    Snake[] snakes;
    SnakeData[] snakesData;
    double timer = 0;

    public SnakeComputer()
    {
        InitializeSnakes(1);
        InitArenaTextures();
        InitSnakeComputeShader();
        explodeComputer = new ExplodeComputer(rd, arenaTexRead, arenaTexWrite);
        pixelSelector = new PixelSelector(rd, arenaTexRead, arenaTexWrite, pxWidth, pxHeight);
    }

    void InitArenaTextures()
    {
        // create a local rendering device.
        rd = RenderingServer.CreateLocalRenderingDevice();

        // create arena read texture format
        var arenaTexReadFormat = new RDTextureFormat();
        arenaTexReadFormat.Format = RenderingDevice.DataFormat.R8G8B8A8Unorm;
        arenaTexReadFormat.Width = pxWidth;
        arenaTexReadFormat.Height = pxHeight;
        arenaTexReadFormat.Depth = 1;
        arenaTexReadFormat.UsageBits =
            RenderingDevice.TextureUsageBits.StorageBit |
            RenderingDevice.TextureUsageBits.CanUpdateBit;

        // create arena write texture format
        var arenaTexWriteFormat = new RDTextureFormat();
        arenaTexWriteFormat.Format = RenderingDevice.DataFormat.R8G8B8A8Unorm;
        arenaTexWriteFormat.Width = pxWidth;
        arenaTexWriteFormat.Height = pxHeight;
        arenaTexWriteFormat.Depth = 1;
        arenaTexWriteFormat.UsageBits =
            RenderingDevice.TextureUsageBits.CanCopyFromBit |
            RenderingDevice.TextureUsageBits.StorageBit |
            RenderingDevice.TextureUsageBits.CanUpdateBit;

        // create arena textures
        arenaTexRead = rd.TextureCreate(arenaTexReadFormat, new RDTextureView());
        arenaTexWrite = rd.TextureCreate(arenaTexWriteFormat, new RDTextureView());
    }

    void InitSnakeComputeShader()
    {
        // load snake GLSL shader
        snakeShaderFile = GD.Load<RDShaderFile>("res://Scripts/SnakeCompute.glsl");
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

    void InitializeSnakes(int snakeCount)
    {
        var rng = new RandomNumberGenerator();
        snakes = new Snake[snakeCount];
        for (int i = 0; i < snakeCount; i++)
        {
            snakes[i] = new Snake();
            //snakes[i].Color = new Color(rng.RandfRange(0, 1), rng.RandfRange(0, 1), rng.RandfRange(0, 1), 1);
            snakes[i].RandomizeStartPos(new Vector2I((int)pxWidth, (int)pxHeight));
        }
        snakesData = new SnakeData[snakes.Length];
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        timer += delta;
        if (timer > 0.5)
        {
            timer = 0;
            Vector2I center = new(200, 200);
            int radius = 1000;
            int[] pixels = pixelSelector.SelectPixels(center, radius);
            explodeComputer.Explode(center, radius, pixels);
        }

        UpdateSnakeData(delta);
        ComputeSnakesSync(snakesData);
        explodeComputer.UpdateExplosion((float)delta);
        DisplayArena();
    }

    void UpdateSnakeData(double delta)
    {
        for (int i = 0; i < snakes.Length; i++)
        {
            snakes[i].Update((float)delta);
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
        rd.ComputeListDispatch(computeList, xGroups: pxWidth / 8, yGroups: pxHeight / 8, zGroups: 1);
        rd.ComputeListEnd();

        // force the GPU to start the commands
        rd.Submit();
    }

    void DisplayArena()
    {
        var texBytes = rd.TextureGetData(arenaTexWrite, 0);
        var arenaImg = Image.CreateFromData((int)pxWidth, (int)pxHeight, false, Image.Format.Rgba8, texBytes);
        var displayTex = ImageTexture.CreateFromImage(arenaImg);
        Texture = displayTex;
        rd.TextureUpdate(arenaTexRead, 0, texBytes);
    }
}
