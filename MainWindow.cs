using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using ProjectOdyssey.Input;
using ProjectOdyssey.Input.Native;
using ProjectOdyssey.Screens;
using System.Diagnostics;

namespace ProjectOdyssey
{
    public class MainWindow : GameWindow
    {
        private readonly ScreenManager screenManager = new();
        private Win32KeyInputListener inputListener = new Win32KeyInputListener();
        private InputHistory inputHistory = new InputHistory();

        // Profiling
        private bool isProfilingEnabled = false;
        private const int MaxFrames = 200_000;
        private static readonly double TicksToMs = 1000.0 / Stopwatch.Frequency;
        private readonly double[] frameTimesMs = new double[MaxFrames];
        private readonly long[] allocatedBytes = new long[MaxFrames];
        private readonly int[] gen2Counts = new int[MaxFrames];
        private readonly string[] screenNames = new string[MaxFrames];   // which screen was active each frame
        private readonly double[] updateMs = new double[MaxFrames];      // time in OnUpdateFrame since the previous render
        private readonly double[] renderMs = new double[MaxFrames];      // our own render work, before SwapBuffers
        private readonly double[] swapMs = new double[MaxFrames];        // time spent inside SwapBuffers (driver / vsync wait)
        private readonly int[] inputCounts = new int[MaxFrames];         // raw key events received since the previous render
        private double updateMsSinceLastRender;
        private int inputEventsSinceLastRender;
        private int frameIndex;

        public MainWindow(int width, int height, string title, bool vsync = true)
            : base(
                new GameWindowSettings
                {
                    UpdateFrequency = vsync ? 0 : 240
                },
                new NativeWindowSettings
                {
                    ClientSize = new Vector2i(width, height),
                    Title = title,
                    APIVersion = new Version(4, 1)
                })
        {
            //WindowState = WindowState.Maximized;

            WindowState = WindowState.Fullscreen;
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
            inputListener.OnInputEvent += _ => Interlocked.Increment(ref inputEventsSinceLastRender); // profiling only

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
            long start = Stopwatch.GetTimestamp();

            base.OnUpdateFrame(args);
            screenManager.Update((float)args.Time * 1000f);

            updateMsSinceLastRender += (Stopwatch.GetTimestamp() - start) * TicksToMs;
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            long renderStart = Stopwatch.GetTimestamp();

            base.OnRenderFrame(args);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            screenManager.Render();

            long beforeSwap = Stopwatch.GetTimestamp();
            SwapBuffers();
            long afterSwap = Stopwatch.GetTimestamp();

            if (!isProfilingEnabled) return;

            if (frameIndex < MaxFrames)
            {
                frameTimesMs[frameIndex] = args.Time * 1000.0;
                allocatedBytes[frameIndex] = GC.GetTotalAllocatedBytes(false);
                gen2Counts[frameIndex] = GC.CollectionCount(2);
                screenNames[frameIndex] = screenManager.CurrentScreen?.GetType().Name ?? "";
                updateMs[frameIndex] = updateMsSinceLastRender;
                renderMs[frameIndex] = (beforeSwap - renderStart) * TicksToMs;
                swapMs[frameIndex] = (afterSwap - beforeSwap) * TicksToMs;
                inputCounts[frameIndex] = Interlocked.Exchange(ref inputEventsSinceLastRender, 0);
                frameIndex++;
            }

            updateMsSinceLastRender = 0;
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
            w.WriteLine("frame,frame_ms,allocated_bytes,gen2_count,screen,update_ms,render_ms,swap_ms,input_events");

            for (int i = 0; i < frameIndex; i++)
            {
                w.WriteLine($"{i},{frameTimesMs[i]:F4},{allocatedBytes[i]},{gen2Counts[i]},{screenNames[i]},{updateMs[i]:F4},{renderMs[i]:F4},{swapMs[i]:F4},{inputCounts[i]}");
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