using Godot;

namespace ADK
{
    public partial class Arena : TextureRect
    {
        [Export]
        uint pxWidth = 512, pxHeight = 512;
        public uint Width => pxWidth;
        public uint Height => pxHeight;

        [Export] RDShaderFile snakeComputeShader, explodeComputeShader, selectComputeShader;

        RenderingDevice rd;
        Rid arenaTexRead, arenaTexWrite;

        SnakeComputer snakeComputer;
        ExplodeComputer explodeComputer;
        PixelSelector pixelSelector;

        public override void _Ready()
        {
            InitArenaTextures();
            snakeComputer = new SnakeComputer(this, rd, snakeComputeShader, arenaTexRead, arenaTexWrite);
            explodeComputer = new ExplodeComputer(rd, explodeComputeShader, arenaTexRead, arenaTexWrite);
            pixelSelector = new PixelSelector(rd, selectComputeShader, arenaTexRead, arenaTexWrite, pxWidth, pxHeight);
        }

        void InitArenaTextures()
        {
            // create a local rendering device.
            rd = RenderingServer.CreateLocalRenderingDevice();

            // create arena read texture format
            var arenaTexReadFormat = new RDTextureFormat
            {
                Format = RenderingDevice.DataFormat.R8G8B8A8Unorm,
                Width = pxWidth,
                Height = pxHeight,
                Depth = 1,
                UsageBits =
                RenderingDevice.TextureUsageBits.StorageBit |
                RenderingDevice.TextureUsageBits.CanUpdateBit
            };

            // create arena write texture format
            var arenaTexWriteFormat = new RDTextureFormat
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
            arenaTexRead = rd.TextureCreate(arenaTexReadFormat, new RDTextureView());
            arenaTexWrite = rd.TextureCreate(arenaTexWriteFormat, new RDTextureView());
        }

        // input events instead of polling
        public override void _Input(InputEvent @event)
        {
            base._Input(@event);
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

        void DisplayArena()
        {
            // unfortunately there is no way to display a gpu texture
            // other then fetch the data and create a cpu texture from it...
            // https://github.com/godotengine/godot-demo-projects/pull/938
            // https://docs.godotengine.org/de/4.x/classes/class_texture2drd.html
            // coming in GODOT 4.2 (?)
            var texBytes = rd.TextureGetData(arenaTexWrite, 0);
            var arenaImg = Image.CreateFromData((int)pxWidth, (int)pxHeight, false, Image.Format.Rgba8, texBytes);
            var displayTex = ImageTexture.CreateFromImage(arenaImg);
            Texture = displayTex;
            //rd.TextureUpdate(arenaTexRead, 0, texBytes);
        }
    }
}