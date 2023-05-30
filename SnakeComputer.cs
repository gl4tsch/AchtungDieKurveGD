using System.IO;
using Godot;
using Godot.Collections;
using System;

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

    public struct SnakeData
    {
        public float prevPosX, prevPosY, newPosX, newPosY;
        public float thickness;
        public float colorR, colorG, colorB, colorA;
        public int collision; // bool

        public byte[] ToByteArray()
        {
            var stream = new MemoryStream();
            var writer = new BinaryWriter(stream);

            writer.Write(this.prevPosX);
            writer.Write(this.prevPosY);
            writer.Write(this.newPosX);
            writer.Write(this.newPosY);
            writer.Write(this.thickness);
            writer.Write(this.colorR);
            writer.Write(this.colorG);
            writer.Write(this.colorB);
            writer.Write(this.colorA);
            writer.Write(this.collision);

            return stream.ToArray();
        }

        public static uint SizeInByte => sizeof(float) * 9 + sizeof(int);
    }

    public SnakeComputer()
    {
        InitializeComputeShader();
    }

    void InitializeComputeShader()
    {
        // create a local rendering device.
        rd = RenderingServer.CreateLocalRenderingDevice();

        // load GLSL shader
        shaderFile = GD.Load<RDShaderFile>("res://snakeCompute.glsl");
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
        snakeBuffer = rd.StorageBufferCreate(SnakeData.SizeInByte); // max 1 snake for now

        // create a snake uniform to assign the snake buffer to the rendering device
        var snakeUniform = new RDUniform
        {
            UniformType = RenderingDevice.UniformType.StorageBuffer,
            Binding = 1
        };
        snakeUniform.AddId(snakeBuffer);

        uniformSet = rd.UniformSetCreate(new Array<RDUniform> { arenaUniform, snakeUniform }, shader, 0);
    }

    public override void _Process(double delta)
    {
        base._Process(delta);

        tempTime += delta;

        var snakesData = new SnakeData[] {
            new SnakeData(){
            prevPosX = 10 + (float)tempTime * 10,
            prevPosY = 10 + (float)tempTime * 10,
            newPosX = 10 + (float)tempTime * 10 + 5,
            newPosY = 10 + (float)tempTime * 10 + 5,
            thickness = 10,
            colorR = 1,
            colorG = 1,
            colorB = 0,
            colorA = 1,
            collision = 0
        } };

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
        var snakesBytes = snakesData[0].ToByteArray();
        rd.BufferUpdate(snakeBuffer, 0, SnakeData.SizeInByte * (uint)snakesData.Length, snakesBytes);

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
