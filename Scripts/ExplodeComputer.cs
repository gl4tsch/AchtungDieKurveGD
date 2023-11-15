using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Godot.Collections;

namespace ADK
{
    public class ExplodeComputer
    {
        RenderingDevice rd;
        Rid arenaTexRead, arenaTexWrite;

        RDShaderFile explosionShaderFile;
        Rid explosionShader;
        Rid explosionPipeline;
        RDUniform arenaInUniform, arenaOutUniform, paramsUniform;
        Rid paramsBuffer;
        List<Explosion> activeExplosions = new();

        uint maxExplodingPixels = 512 * 512;

        public ExplodeComputer(RenderingDevice rd, RDShaderFile computeShader, Rid arenaTexRead, Rid arenaTexWrite)
        {
            this.rd = rd;
            explosionShaderFile = computeShader;
            this.arenaTexRead = arenaTexRead;
            this.arenaTexWrite = arenaTexWrite;
            InitExplodeComputeShader();
        }

        void InitExplodeComputeShader()
        {
            // load explosion GLSL shader
            // explosionShaderFile = GD.Load<RDShaderFile>("res://Scripts/ExplosionCompute.glsl");
            var explosionBytecode = explosionShaderFile.GetSpirV();
            explosionShader = rd.ShaderCreateFromSpirV(explosionBytecode);
            explosionPipeline = rd.ComputePipelineCreate(explosionShader);

            // arena input tex uniform
            arenaInUniform = new RDUniform
            {
                UniformType = RenderingDevice.UniformType.Image,
                Binding = 0 // the in tex
            };
            arenaInUniform.AddId(arenaTexRead);

            // arena output tex uniform
            arenaOutUniform = new RDUniform
            {
                UniformType = RenderingDevice.UniformType.Image,
                Binding = 1 // the out tex
            };
            arenaOutUniform.AddId(arenaTexWrite);

            paramsBuffer = rd.StorageBufferCreate(sizeof(float));

            // create params uniform for things like delta time
            paramsUniform = new RDUniform
            {
                UniformType = RenderingDevice.UniformType.StorageBuffer,
                Binding = 3
            };
            paramsUniform.AddId(paramsBuffer);
        }

        public void Explode(Vector2I center, float radius, Pixel[] pixels)
        {
            // create explody buffer
            var explodyBuffer = rd.StorageBufferCreate(ExplodyPixelData.SizeInByte * maxExplodingPixels);

            // create an explody uniform to assign the explody buffer to the rendering device
            var explodyUniform = new RDUniform
            {
                UniformType = RenderingDevice.UniformType.StorageBuffer,
                Binding = 2
            };
            explodyUniform.AddId(explodyBuffer);

            var explodyUniformSet = rd.UniformSetCreate(new Array<RDUniform> { arenaOutUniform, explodyUniform, paramsUniform }, explosionShader, 0);

            //GD.Print(pixels[0] + " " + pixels[1] + " " + pixels[2] + " " + pixels[3] + " " + pixels[4]);

            var rng = new RandomNumberGenerator();
            List<byte> explodyPixels = new List<byte>();

            for (int i = 0; i < pixels.Length; i ++)
            {
                Pixel px = pixels[i];
                Vector2 pos = new Vector2(px.posX, px.posY);
                Vector2 dir = pos - center;
                //Vector2 dir = new Vector2(rng.RandfRange(-1, 1), rng.RandfRange(-1, 1));
                dir = dir.Normalized();
                dir *= rng.Randf();
                ExplodyPixelData ePx = new()
                {
                    xPos = pos.X,
                    yPos = pos.Y,
                    xDir = dir.X,
                    yDir = dir.Y,
                    r = px.r,
                    g = px.g,
                    b = px.b
                };
                explodyPixels.AddRange(ePx.ToByteArray());
            }

            var explosion = new Explosion
            {
                pixelData = explodyPixels.ToArray(),
                center = center,
                radius = radius,
                duration = 3f,
                explodyUniformSet = explodyUniformSet
            };

            activeExplosions.Add(explosion);
            rd.BufferUpdate(explodyBuffer, 0, (uint)explosion.pixelData.Count(), explosion.pixelData);
            GD.Print("Boom");
        }

        public void UpdateExplosions(float deltaTime)
        {
            if (activeExplosions.Count == 0)
                return;

            rd.BufferUpdate(paramsBuffer, 0, sizeof(float), BitConverter.GetBytes(deltaTime));

            // tick explosions and get rid of old explosions
            List<Explosion> finishedExplosions = new();
            foreach (var explosion in activeExplosions)
            {
                explosion.elapsedTime += deltaTime;
                if (explosion.elapsedTime > explosion.duration)
                {
                    finishedExplosions.Add(explosion);
                }
            }
            finishedExplosions.ForEach(explover => activeExplosions.Remove(explover));

            // run compute shader for all explosions
            foreach (var explosion in activeExplosions)
            {
                var computeList = rd.ComputeListBegin();
                rd.ComputeListBindComputePipeline(computeList, explosionPipeline);
                rd.ComputeListBindUniformSet(computeList, explosion.explodyUniformSet, 0);
                int numGroupsX = Mathf.CeilToInt(explosion.pixelData.Length / (float)16); // 16 = num thread groups x
                rd.ComputeListDispatch(computeList, xGroups: (uint)numGroupsX, yGroups: 1, zGroups: 1);
                rd.ComputeListEnd();
            }

            // force the GPU to start the commands
            rd.Submit();
            rd.Sync();
        }
    }
}