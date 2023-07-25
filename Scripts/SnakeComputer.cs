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
    RDShaderFile snakeShaderFile, explosionShaderFile, pixelSelectShaderFile;
    Rid snakeShader, explosionShader, pixelSelectShader;
    Rid snakePipeline, explosionPipeline, pixelSelectPipeline;
    Rid arenaTexRead, arenaTexWrite;
    Rid snakeUniformSet, explodyUniformSet, pixelSelectUniformSet;
    Rid snakeBuffer, explodyBuffer, paramsBuffer, pxFilterBuffer, selectedPixelsBuffer;

    double tempTime = 0;
    Snake[] snakes;
    SnakeData[] snakesData;
    List<Explosion> activeExplosions = new List<Explosion>();
    uint maxPixelsPerExplosion = 512*512;
    double timer = 0;

    public SnakeComputer()
    {
        InitializeSnakes(2);
        InitArenaTextures();
        InitSnakeComputeShader();
        InitExplodeComputeShader();
        InitPixelSelectComputeShader();
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

    void InitExplodeComputeShader()
    {
        // load explosion GLSL shader
        explosionShaderFile = GD.Load<RDShaderFile>("res://Scripts/ExplosionCompute.glsl");
        var explosionBytecode = explosionShaderFile.GetSpirV();
        explosionShader = rd.ShaderCreateFromSpirV(explosionBytecode);

        explosionPipeline = rd.ComputePipelineCreate(explosionShader);

        // arena input tex uniform
        var arenaInUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.Image,
            Binding = 0 // the in tex
        };
        arenaInUniform.AddId(arenaTexRead);

        // arena output tex uniform
        var arenaOutUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.Image,
            Binding = 1 // the out tex
        };
        arenaOutUniform.AddId(arenaTexWrite);

        // create explody buffer
        explodyBuffer = rd.StorageBufferCreate(ExplodyPixelData.SizeInByte * maxPixelsPerExplosion);
        // create params buffer
        paramsBuffer = rd.StorageBufferCreate(sizeof(float));

        // create an explody uniform to assign the snake buffer to the rendering device
        var explodyUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 2
        };
        explodyUniform.AddId(explodyBuffer);

        // create params uniform for things like delta time
        var paramsUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 3
        };
        paramsUniform.AddId(paramsBuffer);

        explodyUniformSet = rd.UniformSetCreate(new Array<RDUniform> { arenaInUniform, arenaOutUniform, explodyUniform, paramsUniform }, explosionShader, 0);
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
        selectedPixelsBuffer = rd.StorageBufferCreate(sizeof(uint) + sizeof(int)* 2 * maxPixelsPerExplosion);

        // create a uniform to assign the buffer to the rendering device
        var selectedPxUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 2
        };
        selectedPxUniform.AddId(selectedPixelsBuffer);

        pixelSelectUniformSet = rd.UniformSetCreate(new Array<RDUniform> { arenaUniform, pxFilterUniform, selectedPxUniform }, pixelSelectShader, 0);
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
        snakesData = new SnakeData[snakes.Length];
    }

    void Explode(Vector2I center, float radius)
    {
        GD.Print("Boom");
        // List<uint> pixelList = new List<uint>();
        // for (int i = 0; i < 512; i++)
        // {
        //     pixelList.Add((uint)i);
        //     pixelList.Add((uint)i);
        // }
        // var pixels = pixelList.ToArray();

        var pixels = SelectPixels(center, radius);
        //GD.Print(pixels[0] + " " + pixels[1] + " " + pixels[2] + " " + pixels[3] + " " + pixels[4]);

        var rng = new RandomNumberGenerator();
        List<byte> explodyPixels = new List<byte>();

        for (int i = 0; i < pixels.Length-1; i+=2)
        {
            var ePx = new ExplodyPixelData{
                xPos = (float)pixels[i],
                yPos = (float)pixels[i+1],
                xDir = rng.RandfRange(-1, 1),
                yDir = rng.RandfRange(-1, 1),
                r = 1f,
                g = 0f,
                b = 0f
            };
            explodyPixels.AddRange(ePx.ToByteArray());
        }

        var explosion = new Explosion
        {
            pixelData = explodyPixels.ToArray(),
            center = center,
            radius = radius,
            duration = 3f
        };
        activeExplosions.Add(explosion);

        rd.BufferUpdate(explodyBuffer, 0, (uint)explosion.pixelData.Length, explosion.pixelData);
    }

    int[] SelectPixels(Vector2I center, float radius)
    {
        // select the pixels involved in the explosion
        var xBytes = BitConverter.GetBytes(center.X);
        var yBytes = BitConverter.GetBytes(center.Y);
        var rBytes = BitConverter.GetBytes(radius);
        var data = new byte[xBytes.Length + yBytes.Length + rBytes.Length];
        xBytes.CopyTo(data, 0);
        yBytes.CopyTo(data, xBytes.Length);
        rBytes.CopyTo(data, xBytes.Length + yBytes.Length);

        rd.BufferUpdate(pxFilterBuffer, 0, sizeof(int)*2 + sizeof(float), data);
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
        uint numPixels = BitConverter.ToUInt32(byteSize);
        GD.Print("# exploding pixels: " + numPixels);

        // offset by insertion index size
        var pixelData = rd.BufferGetData(selectedPixelsBuffer, sizeof(uint), sizeof(int) * 2 * maxPixelsPerExplosion);
        int[] pixels = new int[numPixels];
        Buffer.BlockCopy(pixelData, 0, pixels, 0, pixels.Length);
        return pixels;
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        timer += delta;
        if (timer > 5)
        {
            timer = 0;
            Explode(new Vector2I(200, 200), 1000);
        }

        UpdateSnakeData(delta);
        ComputeSnakesSync(snakesData);
        UpdateExplosions((float)delta);
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

    void UpdateExplosions(float deltaTime)
    {
        if (activeExplosions.Count == 0)
            return;

        rd.BufferUpdate(paramsBuffer, 0, sizeof(float), BitConverter.GetBytes(deltaTime));

        for (int i = activeExplosions.Count - 1; i >= 0; i--)
        {
            var explosion = activeExplosions[i];
            explosion.elapsedTime += deltaTime;
            if (explosion.elapsedTime > explosion.duration)
            {
                activeExplosions.Remove(explosion);
                continue;
            }

            var computeList = rd.ComputeListBegin();
            rd.ComputeListBindComputePipeline(computeList, explosionPipeline);
            rd.ComputeListBindUniformSet(computeList, explodyUniformSet, 0);
            int numGroupsX = Mathf.CeilToInt(explosion.pixelData.Length / (float)16); // 16 = num thread groups x
            rd.ComputeListDispatch(computeList, xGroups: (uint)numGroupsX, yGroups: 1, zGroups: 1);
            rd.ComputeListEnd();

            // force the GPU to start the commands
            rd.Submit();
            rd.Sync();
        }
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
