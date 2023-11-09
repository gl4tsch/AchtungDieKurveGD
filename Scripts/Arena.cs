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

        public void ExplosionTest()
        {
            Vector2I center = new(200, 200);
            int radius = 1000;
            int[] pixels = pixelSelector.SelectPixels(center, radius);
            explodeComputer.Explode(center, radius, pixels);
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
}