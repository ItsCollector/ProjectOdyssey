using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using ProjectOdyssey.Audio;
using ProjectOdyssey.Input;
using ProjectOdyssey.Input.Native;
using ProjectOdyssey.IO;
using ProjectOdyssey.Screens;
using ProjectOdyssey.Skinning;

namespace ProjectOdyssey
{
    public class MainWindow : GameWindow
    {
        private readonly ScreenManager screenManager = new();
        private Win32KeyInputListener inputListener = new Win32KeyInputListener();
        private InputHistory inputHistory = new InputHistory();

        // Profiling
        private bool isProfilingEnabled = true; 
        private const int MaxFrames = 200_000;
        private readonly double[] frameTimesMs = new double[MaxFrames];
        private readonly long[] allocatedBytes = new long[MaxFrames];
        private readonly int[] gen2Counts = new int[MaxFrames];
        private readonly string[] screenNames = new string[MaxFrames];   // which screen was active each frame
        private int frameIndex;

        public MainWindow(int width, int height, string title, bool vsync = false)
            : base(
                new GameWindowSettings
                {
                    UpdateFrequency = 240
                },
                new NativeWindowSettings
                {
                    ClientSize = new Vector2i(width, height),
                    Title = title,
                    APIVersion = new Version(4, 1)
                })
        {
            WindowState = WindowState.Maximized;
            
            //WindowState = WindowState.Fullscreen;
            Context.SwapInterval = vsync ? 1 : 0;
        }

        protected unsafe override void OnLoad()
        {
            base.OnLoad();
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            inputListener.Initialise((IntPtr)WindowPtr, WndProcHook);
            inputListener.OnInputEvent += inputHistory.RecordInputEvent;

            // Loads all skin textures/glyphs once. Needs the GL context, so it can't happen in the constructor.
            screenManager.Initialise(Path.Combine(AppContext.BaseDirectory, "Skins", "Skin 1"));

            // ScreenManager needs to know the viewport before the first
            // screen is pushed, so Resize() is used here instead of it
            // reading ClientSize on its own.
            screenManager.Resize(ClientSize.X, ClientSize.Y);

            string chartPath = ""; // put the path to your chart file here temporaily 
            string songPath = "";
            //ChartData chartData = ChartBinaryReader.ReadChartBinary(chartPath);

            //screenManager.Push(new GameplayScreen(chartData, songPath, inputHistory, audioManager));
            screenManager.Push(new ChartBrowserScreen(inputHistory));
            //screenManager.Push(new ChartManagerScreen());

            //Task.Run(() => ChartImportService.ImportChartsFromOsu()); // imports all charts from the osu! Songs directory into the Odyssey's own database

            Console.WriteLine($"[INFO] ClientSize = {ClientSize.X} x {ClientSize.Y}");
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);
            screenManager.Update((float)args.Time * 1000f);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            screenManager.Render();
            SwapBuffers();

            if (!isProfilingEnabled) return;

            if (frameIndex < MaxFrames)
            {
                frameTimesMs[frameIndex] = args.Time * 1000.0;
                allocatedBytes[frameIndex] = GC.GetTotalAllocatedBytes(false);
                gen2Counts[frameIndex] = GC.CollectionCount(2);
                screenNames[frameIndex] = screenManager.CurrentScreen?.GetType().Name ?? "";
                frameIndex++;
            }
        }

        protected unsafe override void OnUnload()
        {
            base.OnUnload();

            screenManager.Dispose();   // unloads screens, then the skin, then audio
            inputListener.Dispose((IntPtr)WindowPtr);

            if (!isProfilingEnabled || frameIndex == 0) return;

            // Write profiling data to a CSV file for analysis
            string dir = Path.Combine(AppContext.BaseDirectory, "Profiling");
            Directory.CreateDirectory(dir);   // does nothing if it already exists

            string path = Path.Combine(dir, $"frametimes_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            using var w = new StreamWriter(path);
            w.WriteLine("frame,frame_ms,allocated_bytes,gen2_count,screen");

            for (int i = 0; i < frameIndex; i++)
            {
                w.WriteLine($"{i},{frameTimesMs[i]:F4},{allocatedBytes[i]},{gen2Counts[i]},{screenNames[i]}");
            }

            Console.WriteLine($"[INFO] Wrote {frameIndex} frames to {path}");
        }

        protected override void OnFramebufferResize(FramebufferResizeEventArgs args)
        {
            base.OnFramebufferResize(args);
            GL.Viewport(0, 0, args.Width, args.Height);
            screenManager.Resize(args.Width, args.Height);
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.IsRepeat) return; // ignore OS key-repeat while held
            screenManager.OnKeyDown(e.Key);
        }

        protected override void OnMouseDown(MouseButtonEventArgs e)
        {
            base.OnMouseDown(e);
            screenManager.OnMouseDown(e);
        }

        protected override void OnMouseMove(MouseMoveEventArgs e)
        {
            base.OnMouseMove(e);
            screenManager.OnMouseMove(e);
        }

        protected override void OnMouseWheel(MouseWheelEventArgs e)
        {
            base.OnMouseWheel(e);
            screenManager.OnMouseWheel(e);
        }

        private IntPtr WndProcHook(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == 0x00FF) // WM_INPUT
            {
                inputListener.HandleRawInput(lParam);
            }

            return inputListener.CallNextWindowProc(hWnd, msg, wParam, lParam);
        }
    }
}
