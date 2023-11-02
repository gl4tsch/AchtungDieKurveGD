using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

public class ExplodeComputer
{
    RenderingDevice rd;
    Rid arenaTexRead, arenaTexWrite;

    RDShaderFile explosionShaderFile;
    Rid explosionShader;
    Rid explosionPipeline;
    Rid explodyUniformSet;
    Rid paramsBuffer;
    Rid explodyBuffer;

    uint maxExplodingPixels = 512 * 512;
    Explosion activeExplosion;

    public ExplodeComputer(RenderingDevice rd, Rid arenaTexRead, Rid arenaTexWrite)
    {
        this.rd = rd;
        this.arenaTexRead = arenaTexRead;
        this.arenaTexWrite = arenaTexWrite;
        InitExplodeComputeShader();
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
        explodyBuffer = rd.StorageBufferCreate(ExplodyPixelData.SizeInByte * maxExplodingPixels);
        // create params buffer
        paramsBuffer = rd.StorageBufferCreate(sizeof(float));

        // create an explody uniform to assign the explody buffer to the rendering device
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

    public void Explode(Vector2I center, float radius, int[] pixels)
    {
        GD.Print("Boom");
        //GD.Print(pixels[0] + " " + pixels[1] + " " + pixels[2] + " " + pixels[3] + " " + pixels[4]);

        var rng = new RandomNumberGenerator();
        List<byte> explodyPixels = new List<byte>();

        for (int i = 0; i < pixels.Length; i += 2)
        {
            var ePx = new ExplodyPixelData();
            Vector2 pos = new Vector2((float)pixels[i], (float)pixels[i + 1]);
            //Vector2 dir = pos - center;
            Vector2 dir = new Vector2(rng.RandfRange(-1, 1), rng.RandfRange(-1, 1));
            dir = dir.Normalized();
            dir *= rng.Randf();
            ePx.xPos = pos.X;
            ePx.yPos = pos.Y;
            ePx.xDir = dir.X;
            ePx.yDir = dir.Y;
            ePx.r = 1f;
            ePx.g = 0f;
            ePx.b = 0f;
            explodyPixels.AddRange(ePx.ToByteArray());
        }

        var explosion = new Explosion
        {
            pixelData = explodyPixels.ToArray(),
            center = center,
            radius = radius,
            duration = 3f
        };
        activeExplosion = explosion;

        rd.BufferUpdate(explodyBuffer, 0, (uint)explosion.pixelData.Count(), explosion.pixelData);
    }

    public void UpdateExplosion(float deltaTime)
    {
        if (activeExplosion == null)
            return;

        rd.BufferUpdate(paramsBuffer, 0, sizeof(float), BitConverter.GetBytes(deltaTime));

        activeExplosion.elapsedTime += deltaTime;
        if (activeExplosion.elapsedTime > activeExplosion.duration)
        {
            activeExplosion = null;
        }

        var computeList = rd.ComputeListBegin();
        rd.ComputeListBindComputePipeline(computeList, explosionPipeline);
        rd.ComputeListBindUniformSet(computeList, explodyUniformSet, 0);
        int numGroupsX = Mathf.CeilToInt(activeExplosion.pixelData.Length / (float)16); // 16 = num thread groups x
        rd.ComputeListDispatch(computeList, xGroups: (uint)numGroupsX, yGroups: 1, zGroups: 1);
        rd.ComputeListEnd();

        // force the GPU to start the commands
        rd.Submit();
        rd.Sync();
    }
}