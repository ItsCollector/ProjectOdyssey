using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using ProjectOdyssey.App;
using ProjectOdyssey.Diagnostic;
using ProjectOdyssey.Input;
using ProjectOdyssey.Input.Native;
using ProjectOdyssey.Screens.ChartBrowser;

namespace ProjectOdyssey
{
    public class MainWindow : GameWindow
    {
        private ScreenManager screenManager;
        private Win32KeyInputListener inputListener = new Win32KeyInputListener();
        private InputHistory inputHistory = new InputHistory();
        private readonly FrameProfiler profiler = new(isEnabled: false);

        // Game window settings
        private bool vsyncOn = false;

        public MainWindow(int width, int height, string title, bool vsyncOn = true): base(CreateGameWindowSettings(vsyncOn), CreateNativeWindowSettings(width, height, title))
        {
            screenManager = new ScreenManager(Path.Combine(AppContext.BaseDirectory, "Skins", "Skin 1"));

            WindowState = WindowState.Maximized;
            Context.SwapInterval = vsyncOn ? 1 : 0;
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
            inputListener.OnInputEvent += _ => profiler.CountInputEvent();

           // Task.Run(() => ChartImportService.ImportChartsFromOsu(maxSets: 10)); // imports all charts from the osu! Songs directory into the Odyssey's own database

            // ScreenManager needs to know the viewport before the first
            // screen is pushed, so Resize() is used here instead of it
            // reading ClientSize on its own.
            screenManager.Resize(ClientSize.X, ClientSize.Y);
            screenManager.Push(new ChartBrowserScreen(inputHistory, screenManager.renderer, screenManager, screenManager.audioManager, screenManager.skinManager));

            Console.WriteLine($"[INFO] ClientSize = {ClientSize.X} x {ClientSize.Y}");
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            long start = FrameProfiler.Timestamp();

            base.OnUpdateFrame(args);
            screenManager.Update((float)args.Time * 1000f);

            profiler.RecordUpdate(start);
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            long renderStart = FrameProfiler.Timestamp();

            base.OnRenderFrame(args);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            screenManager.Render();

            long beforeSwap = FrameProfiler.Timestamp();
            SwapBuffers();
            long afterSwap = FrameProfiler.Timestamp();

            profiler.RecordFrame(args.Time * 1000.0, screenManager.CurrentScreen, renderStart, beforeSwap, afterSwap);
        }

        protected unsafe override void OnUnload()
        {
            base.OnUnload();

            screenManager.Dispose();   // unloads screens, then the skin, then audio
            inputListener.Dispose((IntPtr)WindowPtr);

            profiler.WriteCsv(Path.Combine(AppContext.BaseDirectory, "Profiling"));
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

        private void ToggleVsync()
        {
            vsyncOn = !vsyncOn;

            VSync = vsyncOn
                ? VSyncMode.On
                : VSyncMode.Off;
        }

        private static GameWindowSettings CreateGameWindowSettings(bool vsyncOn)
        {
            return new GameWindowSettings
            {
                UpdateFrequency = vsyncOn ? 0 : 240
            };
        }

        private static NativeWindowSettings CreateNativeWindowSettings(int width, int height, string title)
        {
            return new NativeWindowSettings
            {
                ClientSize = new Vector2i(width, height),
                Title = title,
                APIVersion = new Version(4, 1)
            };
        }
    }
}