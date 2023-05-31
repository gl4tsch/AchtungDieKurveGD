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
    RDShaderFile shaderFile;
    Rid shader;
    Rid pipeline;
    Rid arenaTex;
    Rid uniformSet;
    Rid snakeBuffer;

    double tempTime = 0;
    Snake[] snakes;

    public SnakeComputer()
    {
        InitializeSnakes(1000);
        InitializeComputeShader();
    }

    void InitializeComputeShader()
    {
        // create a local rendering device.
        rd = RenderingServer.CreateLocalRenderingDevice();

        // load GLSL shader
        shaderFile = GD.Load<RDShaderFile>("res://Scripts/SnakeCompute.glsl");
        var shaderBytecode = shaderFile.GetSpirV();
        shader = rd.ShaderCreateFromSpirV(shaderBytecode);

        // Create a compute pipeline
        pipeline = rd.ComputePipelineCreate(shader);

        // create arena texture format
        var arenaTexFormat = new RDTextureFormat();
        arenaTexFormat.Format = RenderingDevice.DataFormat.R8G8B8A8Unorm;
        arenaTexFormat.Width = pxWidth;
        arenaTexFormat.Height = pxHeight;
        arenaTexFormat.Depth = 4;
        arenaTexFormat.UsageBits =
            RenderingDevice.TextureUsageBits.StorageBit |
            RenderingDevice.TextureUsageBits.CanUpdateBit |
            RenderingDevice.TextureUsageBits.CanCopyFromBit;
        
        // create arena texture
        arenaTex = rd.TextureCreate(arenaTexFormat, new RDTextureView());

        // arena tex uniform
        var arenaUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.Image,
            Binding = 0
        };
        arenaUniform.AddId(arenaTex);

        // create snake buffer
        snakeBuffer = rd.StorageBufferCreate(SnakeData.SizeInByte * (uint)snakes.Length);

        // create a snake uniform to assign the snake buffer to the rendering device
        var snakeUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 1
        };
        snakeUniform.AddId(snakeBuffer);

        uniformSet = rd.UniformSetCreate(new Array<RDUniform> { arenaUniform, snakeUniform }, shader, 0);
    }

    void InitializeSnakes(int snakeCount)
    {
        var rng = new RandomNumberGenerator();
        snakes = new Snake[snakeCount];
        for (int i = 0; i < snakeCount; i++)
        {
            snakes[i] = new Snake();
            snakes[i].Color = new Color(rng.RandfRange(0, 1), rng.RandfRange(0, 1), rng.RandfRange(0, 1), 1);
            snakes[i].RandomizeStartPos(new Vector2I((int)pxWidth, (int)pxHeight));
        }
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        var snakesData = new SnakeData[snakes.Length];
        for(int i = 0; i < snakes.Length; i++)
        {
            snakes[i].Update((float)delta);
            snakesData[i] = snakes[i].GetComputeData();
        }

        ComputeSync(snakesData);
        DisplayArena();
    }

    void ComputeSync(SnakeData[] snakesData)
    {
        ComputeAsync(snakesData);
        rd.Sync();
    }

    void ComputeAsync(SnakeData[] snakesData)
    {
        // update snake data buffer
        List<byte> snakesBytes = new List<byte>();

        foreach (var data in snakesData)
        {
            snakesBytes.AddRange(data.ToByteArray());
        }
        rd.BufferUpdate(snakeBuffer, 0, (uint)snakesBytes.Count(), snakesBytes.ToArray());

        var computeList = rd.ComputeListBegin();
        rd.ComputeListBindComputePipeline(computeList, pipeline);
        rd.ComputeListBindUniformSet(computeList, uniformSet, 0);
        rd.ComputeListDispatch(computeList, xGroups: pxWidth / 8, yGroups: pxHeight / 8, zGroups: 1);
        rd.ComputeListEnd();

        // force the GPU to start the commands
        rd.Submit();
    }

    void DisplayArena()
    {
        var texBytes = rd.TextureGetData(arenaTex, 0);
        var arenaImg = Image.CreateFromData((int)pxWidth, (int)pxHeight, false, Image.Format.Rgba8, texBytes);
        var displayTex = ImageTexture.CreateFromImage(arenaImg);
        Texture = displayTex;
    }
}
