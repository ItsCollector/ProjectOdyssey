using System.Runtime.InteropServices;

namespace ProjectOdyssey
{
    static class Win32RawInputMethods
    {
        public const int WM_INPUT = 0x00FF;
        public const int GWL_WNDPROC = -4;
        public const uint RIDEV_INPUTSINK = 0x00000100;
        public const uint RID_INPUT = 0x10000003;

        // Retrieves raw keyboard data from the Win32 input message
        [DllImport("user32.dll")]
        public static extern uint GetRawInputData
        (
            IntPtr hRawInput,
            uint uiCommand,
            IntPtr pData,
            ref uint pcbSize,
            uint cbSizeHeader
        );

        // Registers the application to receive raw keyboard input
        [DllImport("user32.dll")]
        public static extern bool RegisterRawInputDevices
        (
            RAWINPUTDEVICE[] pRawInputDevices,
            uint uiNumDevices,
            uint cbSize
        );

        public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        // Replaces the default Win32 window procedure with a custom input hook
        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowLongPtr(IntPtr hwnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll")]
        public static extern IntPtr CallWindowProc(IntPtr lpPrevWndFunc, IntPtr hwnd, uint msg, IntPtr wParam, IntPtr lParam);

        // Retrieves the native Win32 window handle from GLFW/OpenTK
        [DllImport("glfw3", CallingConvention = CallingConvention.Cdecl, ExactSpelling = true)]
        public static extern nint glfwGetWin32Window(nint window);
    }
}
