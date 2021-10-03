using ChaosGame;
using ChaosGraphics;
using Forms = System.Windows.Forms;
using ChaosMath;
using OpenTK.Graphics.OpenGL;

namespace Unstable
{
    public class Game : BaseGame
    {
        public enum InputLayer
        {
            All
        }

        public Graphics graphics;
        public TextureContainer textures;
        public MeshContainer meshes;
        public MaterialContainer materials;
        public ShaderContainer shaders;
        public FontContainer fonts;
        public ChaosArchive archive;
        public FontContainer.Entry defaultFont;
        public FontManager fontManager;
        public AnimationContainer animations;

        public ChaosSound.SampleContainer samples;
        public ChaosSound.SoundPool soundPool;
        public ChaosSound.Audio audio;

        public ChaosInput.Input input;
        ChaosSound.Music music;

        System.Drawing.Rectangle CalculateWindowedBounds()
        {
            System.Drawing.Rectangle workingArea = Forms.Screen.PrimaryScreen.WorkingArea;
            intVector2 wh = Confix.scaleWindowToScreen ? (intVector2)workingArea.Size : Confix.windowSize;
            int posX = (workingArea.Right - workingArea.Left) / 2 - wh.x / 2;
            int posY = (workingArea.Bottom - workingArea.Top) / 2 - wh.y / 2;
            return new System.Drawing.Rectangle(posX, posY, wh.x, wh.y);
        }

        public Game()
        {
            window.StartPosition = Forms.FormStartPosition.Manual;
            window.Bounds = Forms.Screen.PrimaryScreen.Bounds;
            window.Icon = Properties.Resources.icon;
            window.BackColor = System.Drawing.Color.Black;
            window.BackgroundImage = Properties.Resources.loading;
            window.BackgroundImageLayout = Forms.ImageLayout.Center;
            window.FormBorderStyle = Confix.fullScreen ? Forms.FormBorderStyle.None : Forms.FormBorderStyle.Sizable;
            window.Bounds = Confix.fullScreen ? Forms.Screen.PrimaryScreen.Bounds : CalculateWindowedBounds();
            Forms.Cursor.Hide();
        }

        public override void LoadGame()
        {
            base.LoadGame();
            input = new ChaosInput.Input(typeof(InputLayer));
            input.UpdateDeviceList();

            if (!System.IO.File.Exists("data.cha"))
            {
                var sourceDirectory = System.IO.Directory.GetParent(Forms.Application.ExecutablePath).Parent.Parent;
                ChaosArchive.CreateArchive(sourceDirectory.FullName + "/_DATA",
                    System.IO.Directory.GetFiles(sourceDirectory.FullName + "/_DATA", "*", System.IO.SearchOption.AllDirectories),
                    "data.cha");
            }
            archive = new ChaosArchive("data.cha", false);

            graphics = new Graphics(window, 3, 0);

            textures = new TextureContainer(graphics.dispatcher, false);
            textures.backupArchive = archive;
            textures.LoadDirectory("./", new[] { ".png" }, true, this);

            meshes = new MeshContainer(graphics.dispatcher, false);
            meshes.backupArchive = archive;
            meshes.LoadDirectory("./", new[] { ".gmdl" }, true, this);
            meshes.LoadDirectory("./", MeshLoadFlags.Animated, new[] { ".agmdl" }, true, this);

            animations = new AnimationContainer(false);
            animations.backupArchive = archive;
            animations.LoadDirectory("./", new[] { ".anim" }, true, this);

            materials = new MaterialContainer(graphics, textures, false);
            materials.backupArchive = archive;
            materials.LoadDirectory("./", new[] { ".mat" }, true, this);

            shaders = new ShaderContainer(graphics);
            shaders.backupArchive = archive;
            shaders.LoadDirectory("./", new[] { ".fx" }, true, this);

            fonts = new FontContainer(graphics, false);
            fonts.backupArchive = archive;
            fonts.LoadDirectory("./", new[] { ".chf2" }, true, this);
            defaultFont = fonts.Load("defaultfont.chf2");
            fontManager = new FontManager(graphics, 2048, 2048 * 8, 2048, false, new[] { defaultFont.content });

            audio = new ChaosSound.Audio();
            samples = new ChaosSound.SampleContainer(false);
            samples.backupArchive = archive;
            samples.LoadDirectory("./", new[] { ".wav" }, true, this);
            soundPool = new ChaosSound.SoundPool(audio, samples);
            
            music = new ChaosSound.Music(audio, new System.IO.MemoryStream(archive.LoadFile("soundtrack.ogg")), true);
            music.Play(0.333f);

            scenes.Add(new WorldScene(this));
        }

        System.Diagnostics.Stopwatch tm = new System.Diagnostics.Stopwatch();

        public override void UpdateGame()
        {
            if (Util.GetActiveWindow() == window.Handle)
                Forms.Cursor.Position = (intVector2)((intBoundingRect)window.ClientRectangle).center;
            input.UpdateInputConsumption();
            base.UpdateGame();
        }

        public override void DrawGame()
        {
            tm.Stop();

            tm.Restart();
            graphics.device.SwapBuffers();
            GL.ClearColor(0, 0.3f, 0, 1);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            base.DrawGame();
            Graphics.ThrowErrors();
        }

        protected override void DoDispose()
        {
            base.DoDispose();
            input.Dispose();
            music.Dispose();
            soundPool.Dispose();
            audio.Dispose();
            System.Diagnostics.Process.GetCurrentProcess().Kill();
        }
    }
}
