using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;

namespace ProjectOdyssey
{
    public class MainWindow : GameWindow
    {
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
            Context.SwapInterval = vsync ? 1 : 0;
        }

        protected unsafe override void OnLoad()
        {
            base.OnLoad();

            GL.ClearColor(0.051f, 0.051f, 0.051f, 1.0f);
            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            inputListener.Initialise((IntPtr)WindowPtr, WndProcHook);

            inputListener.OnInputEvent += (inputEvent) =>
            {
                inputHistory.RecordInputEvent(inputEvent);
            };
        }

        protected override void OnUpdateFrame(FrameEventArgs args)
        {
            base.OnUpdateFrame(args);

            if (inputHistory.TryGetNextEvent(out InputEvent inputEvent))
            {
                Console.WriteLine($"[Input] VKey={inputEvent.VKey}, IsPressed={inputEvent.IsPressed}, TimeStamp={inputEvent.TimeStamp}");
            }
        }

        protected override void OnRenderFrame(FrameEventArgs args)
        {
            base.OnRenderFrame(args);
            GL.Clear(ClearBufferMask.ColorBufferBit);
            SwapBuffers();
        }

        protected unsafe override void OnUnload()
        {
            base.OnUnload();
            inputListener.Dispose((IntPtr)WindowPtr);
        }

        protected override void OnFramebufferResize(FramebufferResizeEventArgs args)
        {
            base.OnFramebufferResize(args);
            GL.Viewport(0, 0, args.Width, args.Height);
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
