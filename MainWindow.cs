using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using ProjectOdyssey.App;
using ProjectOdyssey.Diagnostic;
using ProjectOdyssey.Diagnostics;
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
        private readonly FpsCounter fpsCounter = new FpsCounter();
        private readonly FrameLimiter frameLimiter = new();

        public MainWindow(int width, int height, string title)
            : base(CreateGameWindowSettings(), CreateNativeWindowSettings(width, height, title))
        {
            screenManager = new ScreenManager(Path.Combine(AppContext.BaseDirectory, "Skins", "Skin 1"), fpsCounter);

            WindowState = WindowState.Normal;
            GameSettings.Current.Changed += ApplySettings;
        }

        protected unsafe override void OnLoad()
        {
            base.OnLoad();
            ApplySettings();
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GameSettings.Current.ActualWidth = FramebufferSize.X;
            GameSettings.Current.ActualHeight = FramebufferSize.Y;

            inputListener.Initialise((IntPtr)WindowPtr, WndProcHook);
            inputListener.OnInputEvent += inputHistory.RecordInputEvent;
            inputListener.OnInputEvent += _ => profiler.CountInputEvent();

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
            fpsCounter.Tick(args.Time);

            base.OnRenderFrame(args);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            screenManager.Render();

            long beforeSwap = FrameProfiler.Timestamp();
            SwapBuffers();
            long afterSwap = FrameProfiler.Timestamp();

            profiler.RecordFrame(args.Time * 1000.0, screenManager.CurrentScreen, renderStart, beforeSwap, afterSwap);
            frameLimiter.Wait();
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

            GameSettings.Current.ActualWidth = args.Width;   
            GameSettings.Current.ActualHeight = args.Height;
        }

        protected override void OnKeyDown(KeyboardKeyEventArgs e)
        {
            base.OnKeyDown(e);
            if (e.IsRepeat) return; // ignore OS key-repeat while held
            screenManager.OnKeyDown(e.Key, e.Modifiers);
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

        protected override void OnTextInput(TextInputEventArgs e)
        {
            base.OnTextInput(e);
            screenManager.OnTextInput(e);
        }

        private IntPtr WndProcHook(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            if (msg == 0x00FF) // WM_INPUT
            {
                inputListener.HandleRawInput(lParam);
            }

            return inputListener.CallNextWindowProc(hWnd, msg, wParam, lParam);
        }

        private DisplayMode? lastDisplayMode;
        private int lastResolutionIndex = -1;

        private void ApplySettings()
        {
            var s = GameSettings.Current;

            // Apply VSync and frame limiter settings
            if (s.VSync == true)
            {
                VSync = VSyncMode.On;
                frameLimiter.TargetFps = 0;
            }
            else
            {
                VSync = VSyncMode.Off;
                frameLimiter.TargetFps = s.MaxFps;
            }

            // Changed fires for every setting (volume, offset...), so only touch the window when the display settings changed
            if (s.DisplayMode == lastDisplayMode && s.ResolutionIndex == lastResolutionIndex) return;
            lastDisplayMode = s.DisplayMode;
            lastResolutionIndex = s.ResolutionIndex;

            var (w, h) = GameSettings.Resolutions[s.ResolutionIndex];
            var clientRect = Monitors.GetMonitorFromWindow(this).ClientArea;

            // Apply display mode settings
            switch (s.DisplayMode)
            {
                case DisplayMode.Windowed:
                    if (w > clientRect.Size.X || h > clientRect.Size.Y) return;

                    WindowState = WindowState.Normal;
                    WindowBorder = WindowBorder.Fixed;
                    ClientSize = new Vector2i(w, h);
                    Location = (Vector2i)(Monitors.GetMonitorFromWindow(this).ClientArea.Center - new Vector2i(w / 2, h / 2));
                    break;

                case DisplayMode.Borderless:
                    WindowState = WindowState.Normal;
                    WindowBorder = WindowBorder.Hidden;
                    ClientRectangle = Monitors.GetMonitorFromWindow(this).ClientArea;
                    break;

                case DisplayMode.Fullscreen:
                    WindowBorder = WindowBorder.Resizable;
                    WindowState = WindowState.Fullscreen;
                    break;
            }
        }

        private static GameWindowSettings CreateGameWindowSettings()
        {
            return new GameWindowSettings
            {
                UpdateFrequency = 0
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