using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using ProjectOdyssey.Input;
using ProjectOdyssey.Input.Native;
using ProjectOdyssey.IO;
using ProjectOdyssey.Screens;

namespace ProjectOdyssey
{
    public class MainWindow : GameWindow
    {
        private readonly ScreenManager screenManager = new();
        private Win32KeyInputListener inputListener = new Win32KeyInputListener();
        private InputHistory inputHistory = new InputHistory();

        public MainWindow(int width, int height, string title, bool vsync = true)
            : base(
                GameWindowSettings.Default,
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
            GL.ClearColor(0.051f, 0.051f, 0.051f, 1.0f);
            GL.Viewport(0, 0, ClientSize.X, ClientSize.Y);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            inputListener.Initialise((IntPtr)WindowPtr, WndProcHook);
            inputListener.OnInputEvent += inputHistory.RecordInputEvent;

            // ScreenManager needs to know the viewport before the first
            // screen is pushed, so Resize() is used here instead of it
            // reading ClientSize on its own.
            screenManager.Resize(ClientSize.X, ClientSize.Y);

            //string fileName = "Ibuki Kido & Erii Yamazaki - pupa (TV Size) (MapleSyrup-) [Metamorphosis].json";
            //string fileName = "Lime - Pixel Planet (-NoName-) [Advanced].osu";
            string fileName = "test_single_long_note.json";

            ChartData chartData = ChartFileReader.LoadChart(fileName);
            //var result = ChartImporter.Import(Path.Combine(AppContext.BaseDirectory, "Test charts", fileName));

            screenManager.Push(new GameplayScreen(chartData, inputHistory));

            //screenManager.Push(new ChartManagerScreen());

            Console.WriteLine($"ClientSize = {ClientSize.X} x {ClientSize.Y}");
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
        }

        protected unsafe override void OnUnload()
        {
            base.OnUnload();

            screenManager.UnloadAll();
            inputListener.Dispose((IntPtr)WindowPtr);
        }

        protected override void OnFramebufferResize(FramebufferResizeEventArgs args)
        {
            base.OnFramebufferResize(args);
            GL.Viewport(0, 0, args.Width, args.Height);
            screenManager.Resize(args.Width, args.Height);
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
