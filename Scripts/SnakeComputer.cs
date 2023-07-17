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
    RDShaderFile snakeShaderFile, explosionShaderFile;
    Rid snakeShader, explosionShader;
    Rid snakePipeline, explosionPipeline;
    Rid arenaTex;
    Rid snakeUniformSet, explodyUniformSet;
    Rid snakeBuffer, explodyBuffer, paramsBuffer;

    double tempTime = 0;
    Snake[] snakes;
    int testExplodyPxCount = 511;

    public SnakeComputer()
    {
        InitializeSnakes(2);
        InitializeComputeShaders();
        ExplodyInit();
    }

    void InitializeComputeShaders()
    {
        // create a local rendering device.
        rd = RenderingServer.CreateLocalRenderingDevice();

        // load snake GLSL shader
        snakeShaderFile = GD.Load<RDShaderFile>("res://Scripts/SnakeCompute.glsl");
        var snakeBytecode = snakeShaderFile.GetSpirV();
        snakeShader = rd.ShaderCreateFromSpirV(snakeBytecode);

        // load explosion GLSL shader
        explosionShaderFile = GD.Load<RDShaderFile>("res://Scripts/ExplosionCompute.glsl");
        var explosionBytecode = explosionShaderFile.GetSpirV();
        explosionShader = rd.ShaderCreateFromSpirV(explosionBytecode);

        // Create a compute pipelines
        snakePipeline = rd.ComputePipelineCreate(snakeShader);
        explosionPipeline = rd.ComputePipelineCreate(explosionShader);

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
        // create explody buffer
        explodyBuffer = rd.StorageBufferCreate(ExplodyPixelData.SizeInByte * (uint)testExplodyPxCount);
        // create params buffer
        paramsBuffer = rd.StorageBufferCreate(sizeof(float));

        // create a snake uniform to assign the snake buffer to the rendering device
        var snakeUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 1
        };
        snakeUniform.AddId(snakeBuffer);

        // create an explody uniform to assign the snake buffer to the rendering device
        var explodyUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 1
        };
        explodyUniform.AddId(explodyBuffer);

        // create params uniform for things like delta time
        var paramsUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 2
        };
        paramsUniform.AddId(paramsBuffer);

        snakeUniformSet = rd.UniformSetCreate(new Array<RDUniform> { arenaUniform, snakeUniform }, snakeShader, 0);
        explodyUniformSet = rd.UniformSetCreate(new Array<RDUniform> { arenaUniform, explodyUniform, paramsUniform}, explosionShader, 0);
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

        ComputeSnakesSync(snakesData);
        ExplodyTest((float)delta);
        DisplayArena();
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

    void ExplodyInit()
    {
        // update explosion data buffer
        List<byte> explodyBytes = new List<byte>();
        var rng = new RandomNumberGenerator();

        for (int i = 0; i < testExplodyPxCount; i++)
        {
            var px = new ExplodyPixelData();
            px.xPos = i;
            px.yPos = i;
            px.xDir = rng.RandfRange(-1, 1);
            px.yDir = rng.RandfRange(-1, 1);
            px.r = 1;
            explodyBytes.AddRange(px.ToByteArray());
        }
        rd.BufferUpdate(explodyBuffer, 0, (uint)testExplodyPxCount * ExplodyPixelData.SizeInByte, explodyBytes.ToArray());
    }

    // explision tests
    void ExplodyTest(float deltaTime)
    {
        rd.BufferUpdate(paramsBuffer, 0, sizeof(float), new byte[]{(byte)deltaTime});

        var computeList = rd.ComputeListBegin();
        rd.ComputeListBindComputePipeline(computeList, explosionPipeline);
        rd.ComputeListBindUniformSet(computeList, explodyUniformSet, 0);
        int numGroupsX = Mathf.CeilToInt(testExplodyPxCount / (float)16); // 16 = num thread groups x
        rd.ComputeListDispatch(computeList, xGroups: (uint)numGroupsX, yGroups: 1, zGroups: 1);
        rd.ComputeListEnd();

        // force the GPU to start the commands
        rd.Submit();
        rd.Sync();
    }

    void DisplayArena()
    {
        var texBytes = rd.TextureGetData(arenaTex, 0);
        var arenaImg = Image.CreateFromData((int)pxWidth, (int)pxHeight, false, Image.Format.Rgba8, texBytes);
        var displayTex = ImageTexture.CreateFromImage(arenaImg);
        Texture = displayTex;
    }
}

public struct ExplodyPixelData
{
    public float xPos, yPos;
    public float xDir, yDir;
    public float r, g, b;
    public static uint SizeInByte => sizeof(float) * 7;

    public byte[] ToByteArray()
    {
        var stream = new MemoryStream();
        var writer = new BinaryWriter(stream);

        writer.Write(this.xPos);
        writer.Write(this.yPos);
        writer.Write(this.xDir);
        writer.Write(this.yDir);
        writer.Write(this.r);
        writer.Write(this.g);
        writer.Write(this.b);

        return stream.ToArray();
    }
}