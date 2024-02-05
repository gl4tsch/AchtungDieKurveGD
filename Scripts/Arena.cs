using System.Collections.Generic;
using ADK.Net;
using Godot;
using Godot.Collections;

namespace ADK
{
    public partial class Arena : TextureRect
    {
        [Export]
        uint pxWidth = 1024, pxHeight = 1024;
        public uint Width => pxWidth;
        public uint Height => pxHeight;
        public Vector2 Dimensions => new Vector2(Width, Height);

        public static readonly string WidthSettingName = "PxWidth";
        public static readonly string HeightSettingName = "PxHeight";

        public static System.Collections.Generic.Dictionary<string, Variant> DefaultSettings => new()
        {
            {WidthSettingName, 1024},
            {HeightSettingName, 1024}
        };

        [Export] RDShaderFile snakeComputeShader, explodeComputeShader, selectComputeShader, clearTextureComputeShader;

        RenderingDevice rd;
        Rid arenaTexReadWrite;
        Texture2Drd renderTex;

        SnakeComputer snakeComputer;
        ExplodeComputer explodeComputer;
        PixelSelector pixelSelector;

        Rid texClearShader;
        Rid texClearPipeline;
        Rid texClearUniformSet;

        public override void _Ready()
        {
            base._Ready();
            pxWidth = (uint)GameManager.Instance.Settings.ArenaSettings.Settings[WidthSettingName];
            pxHeight = (uint)GameManager.Instance.Settings.ArenaSettings.Settings[HeightSettingName];
            InitArenaTextures();
        }

        public override void _ExitTree()
        {
            base._ExitTree();

            if (rd.TextureIsValid(arenaTexReadWrite))
            {
                rd.FreeRid(arenaTexReadWrite);
            }
            if (texClearShader.IsValid)
            {
                rd.FreeRid(texClearShader);
            }
            if (rd.RenderPipelineIsValid(texClearPipeline))
            {
                rd.FreeRid(texClearPipeline);
            }
            if (rd.UniformSetIsValid(texClearUniformSet))
            {
                rd.FreeRid(texClearUniformSet);
            }
        }

        public void Init(int snakeCount)
        {
            snakeComputer = new SnakeComputer(Width, Height, snakeCount, rd, snakeComputeShader, arenaTexReadWrite);
            explodeComputer = new ExplodeComputer(rd, explodeComputeShader, arenaTexReadWrite);
            pixelSelector = new PixelSelector(rd, selectComputeShader, arenaTexReadWrite, pxWidth, pxHeight);

            ResetArena();
        }

        void InitArenaTextures()
        {
            // with a local rendering device, arenaTexReadWrite cannot be used as the texture in textureRect
            // rd ??= RenderingServer.CreateLocalRenderingDevice();
            rd ??= RenderingServer.GetRenderingDevice();

            // create arena write texture format
            var arenaTexReadWriteFormat = new RDTextureFormat
            {
                Format = RenderingDevice.DataFormat.R8G8B8A8Unorm,
                TextureType = RenderingDevice.TextureType.Type2D,
                Width = pxWidth,
                Height = pxHeight,
                Depth = 1,
                ArrayLayers = 1,
                Mipmaps = 1,
                UsageBits =
                RenderingDevice.TextureUsageBits.SamplingBit |
                RenderingDevice.TextureUsageBits.CanCopyFromBit |
                RenderingDevice.TextureUsageBits.StorageBit |
                RenderingDevice.TextureUsageBits.CanUpdateBit
            };

            // create arena textures
            arenaTexReadWrite = rd.TextureCreate(arenaTexReadWriteFormat, new RDTextureView(), new Array<byte[]>());
            // the old texture should be cleaned up automatically if there are no more references to it
            Texture = new Texture2Drd();
            renderTex = Texture as Texture2Drd;
            renderTex.TextureRdRid = arenaTexReadWrite;
            InitArenaClearComputeShader();
            ClearArenaTextures();
        }

        void InitArenaClearComputeShader()
        {
            // load shader
            var texClearShaderFile = clearTextureComputeShader.GetSpirV();
            texClearShader = rd.ShaderCreateFromSpirV(texClearShaderFile);

            // Create a compute pipeline
            texClearPipeline = rd.ComputePipelineCreate(texClearShader);

            // arena tex uniform
            var arenaUniform = new RDUniform
            {
                UniformType = RenderingDevice.UniformType.Image,
                Binding = 0
            };
            arenaUniform.AddId(arenaTexReadWrite);

            texClearUniformSet = rd.UniformSetCreate(new Array<RDUniform>{ arenaUniform }, texClearShader, 0);
        }

        void ClearArenaTextures()
        {
            var computeList = rd.ComputeListBegin();
            rd.ComputeListBindComputePipeline(computeList, texClearPipeline);
            rd.ComputeListBindUniformSet(computeList, texClearUniformSet, 0);
            rd.ComputeListDispatch(computeList, xGroups: pxWidth / 8, yGroups: pxHeight / 8, zGroups: 1);
            rd.ComputeListEnd();

            // force the GPU to start the commands
            //rd.Submit();
            // wait for GPU
            //rd.Sync();
            rd.Barrier(RenderingDevice.BarrierMask.Compute);
        }

        public void ResetArena()
        {
            ClearArenaTextures();
            explodeComputer.Reset();
        }

        public override void _Process(double delta)
        {
            explodeComputer?.UpdateExplosions((float)delta);
        }

        /// <returns>collided snakes</returns>
        public List<Snake> DrawSnakesAndLines(List<Snake> aliveSnakes)
        {
            List<LineData> snakeDrawData = new();
            List<LineData> lineDrawData = new();
            foreach (var snake in aliveSnakes)
            {
                // snake draw data
                snakeDrawData.Add(snake.GetSnakeDrawData());
                // fill line draw buffer
                foreach (var line in snake.GetLineDrawData())
                {
                    lineDrawData.Add(line);
                }
            }
            snakeComputer.Draw(snakeDrawData, lineDrawData);

            // collisions
            List<Snake> collidedSnakes = new();
            int[] collisions = snakeComputer.GetCollisions();
            for (int i = 0; i < aliveSnakes.Count; i++)
            {
                if (collisions[i] != 0)
                collidedSnakes.Add(aliveSnakes[i]);
            }
            return collidedSnakes;
        }

        public void ExplodeWholeScreen()
        {
            ExplodePixels(new Vector2I((int)pxWidth / 2, (int)pxHeight / 2), (int)pxWidth + (int)pxHeight);
        }

        public void ExplodePixels(Vector2I center, int radius)
        {
            Pixel[] pixels = pixelSelector.SelectPixels(center, radius);
            if (pixels.Length > 0)
            {
                explodeComputer.Explode(center, radius, pixels);
            }
        }

        public void ExplodePixels(Vector2 startPos, Vector2 endPos, float halfThickness)
        {
            Pixel[] pixels = pixelSelector.SelectPixels(startPos, endPos, halfThickness, 1);
            if (pixels.Length > 0)
            {
                explodeComputer.Explode((Vector2I)startPos, halfThickness, pixels);
            }
        }

        public void ExplodePixels(LineFilter pixelFilter)
        {
            Pixel[] pixels = pixelSelector.SelectPixels(pixelFilter);
            if (pixels.Length > 0)
            {
                explodeComputer.Explode(new Vector2I((int)pixelFilter.startPosX, (int)pixelFilter.startPosY), pixelFilter.halfThickness, pixels);
            }
        }
    }
}