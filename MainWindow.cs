using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using ProjectOdyssey.Audio;
using ProjectOdyssey.Input;
using ProjectOdyssey.Input.Native;
using ProjectOdyssey.IO;
using ProjectOdyssey.Screens;

namespace ProjectOdyssey
{
    public class MainWindow : GameWindow
    {
        private readonly ScreenManager screenManager = new();
        private AudioManager audioManager = new();
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

            string chartPath = "C:\\Users\\Evan\\Documents\\GitHub\\ProjectOdyssey\\bin\\Debug\\net8.0\\Charts\\1 VA - Kyukon's 7k Chordjack Practice Pack 2\\V.A - Kyukon's 7K Chordjack Practice Pack 2 (LuKnight) [I Love It].chart"; // put the path to your chart file here temporaily 
            string songPath = "C:\\Users\\Evan\\AppData\\Local\\osu!\\Songs\\1 VA - Kyukon's 7k Chordjack Practice Pack 2\\iloveit.mp3";
            ChartData chartData = ChartBinaryReader.ReadChartBinary(chartPath);

            //screenManager.Push(new GameplayScreen(chartData, songPath, inputHistory, audioManager));
            screenManager.Push(new ChartBrowser());
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
