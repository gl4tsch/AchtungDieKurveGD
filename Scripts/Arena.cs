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
            GameManager.Instance.ActiveArenaScene.BattleStateChanged += OnBattleStateChanged;

            InitArenaTextures();

            snakeComputer = new SnakeComputer(this, rd, snakeComputeShader, arenaTexReadWrite);
            explodeComputer = new ExplodeComputer(rd, explodeComputeShader, arenaTexReadWrite);
            pixelSelector = new PixelSelector(rd, selectComputeShader, arenaTexReadWrite, pxWidth, pxHeight);

            ResetArena();
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

        void InitArenaTextures()
        {
            // create a local rendering device.
            rd ??= RenderingServer.CreateLocalRenderingDevice();

            // create arena write texture format
            var arenaTexReadWriteFormat = new RDTextureFormat
            {
                Format = RenderingDevice.DataFormat.R8G8B8A8Unorm,
                Width = pxWidth,
                Height = pxHeight,
                Depth = 1,
                UsageBits =
                RenderingDevice.TextureUsageBits.CanCopyFromBit |
                RenderingDevice.TextureUsageBits.StorageBit |
                RenderingDevice.TextureUsageBits.CanUpdateBit
            };

            // create arena textures
            arenaTexReadWrite = rd.TextureCreate(arenaTexReadWriteFormat, new RDTextureView());
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
            rd.Submit();
            // wait for GPU
            rd.Sync();
        }

        void ResetArena()
        {
            ClearArenaTextures();
            snakeComputer.Reset();
            explodeComputer.Reset();
        }

        // input events instead of polling
        public override void _Input(InputEvent @event)
        {
            base._Input(@event);
            // pass keyboard inputs to snakeComputer
            if (@event is InputEventKey keyEvent && !keyEvent.IsEcho())
            {
                snakeComputer.HandleSnakeInput(keyEvent);
            }
        }

        public override void _Process(double delta)
        {
            base._Process(delta);
            snakeComputer.UpdateSnakes(delta);
            explodeComputer.UpdateExplosions((float)delta);
            DisplayArena();
        }

        void OnBattleStateChanged(ArenaScene.BattleState battleState)
        {
            if (battleState == ArenaScene.BattleState.StartOfRound)
            {
                ResetArena();
                return;
            }
            else if (battleState == ArenaScene.BattleState.EndOfRound)
            {
                //EndRound();
                return;
            }
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

        void DisplayArena()
        {
            // unfortunately there is no way to display a gpu texture
            // other then fetch the data and create a cpu texture from it...
            // https://github.com/godotengine/godot-demo-projects/pull/938
            // https://docs.godotengine.org/de/4.x/classes/class_texture2drd.html
            // coming in GODOT 4.2 (?)
            var texBytes = rd.TextureGetData(arenaTexReadWrite, 0);
            var arenaImg = Image.CreateFromData((int)pxWidth, (int)pxHeight, false, Image.Format.Rgba8, texBytes);
            var displayTex = ImageTexture.CreateFromImage(arenaImg);
            Texture = displayTex;
            //rd.TextureUpdate(arenaTexRead, 0, texBytes);
        }
    }
}