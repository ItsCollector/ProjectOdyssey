using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ProjectOdyssey
{
    class Win32KeyInputListener
    {
        private Win32RawInputMethods.WndProc? inputHookDelegate;
        private IntPtr originalWndProc;

        private const int RIM_TYPEKEYBOARD = 1;
        private const int RI_KEY_BREAK = 0x0001;

        private HashSet<ushort> keysDown = new HashSet<ushort>(); // keys currently held down
        public event Action<InputEvent, HashSet<ushort>>? OnInputEvent;

        //Hooks into the Windows Message Loop to intercept Raw Input.
        public void Initialise(nint glfwHandle, Win32RawInputMethods.WndProc hookDelegate)
        {
            nint hWnd = Win32RawInputMethods.glfwGetWin32Window(glfwHandle);

            // Keep reference alive to prevent Garbage Collection 
            inputHookDelegate = hookDelegate;

            // 'SetWindowLongPtr' returns the original procedure so we can chain them
            originalWndProc = Win32RawInputMethods.SetWindowLongPtr
            (
                hWnd,
                Win32RawInputMethods.GWL_WNDPROC,
                Marshal.GetFunctionPointerForDelegate(inputHookDelegate)
            );

            bool success = RegisterKeyboardDevice(hWnd);

            if (!success)
            {
                Console.WriteLine("[Input] Failed to register raw input device.");
            }

            Console.WriteLine($"[Input] Hooked HWND: 0x{hWnd:X}");
        }

        private bool RegisterKeyboardDevice(nint targetHwnd)
        {
            RAWINPUTDEVICE[] devices = new RAWINPUTDEVICE[1];
            devices[0].usUsagePage = 0x01;
            devices[0].usUsage = 0x06; // Keyboard
            devices[0].dwFlags = Win32RawInputMethods.RIDEV_INPUTSINK;
            devices[0].hwndTarget = targetHwnd;

            return Win32RawInputMethods.RegisterRawInputDevices(
                devices,
                (uint)devices.Length,
                (uint)Marshal.SizeOf<RAWINPUTDEVICE>()
            );
        }

        public void HandleRawInput(IntPtr lParam)
        {
            uint size = 0;

            // Initial call retrieves required buffer size
            Win32RawInputMethods.GetRawInputData(lParam, Win32RawInputMethods.RID_INPUT, IntPtr.Zero, ref size, (uint)Marshal.SizeOf<RAWINPUTHEADER>());

            unsafe
            {
                byte* pBuffer = stackalloc byte[(int)size];
                IntPtr bufferPtr = (IntPtr)pBuffer;

                if (Win32RawInputMethods.GetRawInputData(lParam, Win32RawInputMethods.RID_INPUT, bufferPtr, ref size, (uint)Marshal.SizeOf<RAWINPUTHEADER>()) != uint.MaxValue)
                {
                    RAWINPUT input = Marshal.PtrToStructure<RAWINPUT>(bufferPtr);
                    ProcessInput(input);
                }
            }
        }

        private void ProcessInput(RAWINPUT input)
        {
            if (input.header.dwType == RIM_TYPEKEYBOARD)
            {
                var kb = input.keyboard;
                bool isPressed = (kb.Flags & RI_KEY_BREAK) == 0;
                ushort key = kb.VKey;

                if (isPressed)
                {
                    // Prevents key repeat spam from generating extra input events
                    if (keysDown.Contains(key))
                        return;

                    keysDown.Add(key);
                }
                else
                {
                    keysDown.Remove(key);
                }

                InputEvent inputEvent = new InputEvent
                {
                    VKey = key,
                    IsPressed = isPressed,
                    TimeStamp = Stopwatch.GetTimestamp(),
                };

                OnInputEvent?.Invoke(inputEvent, keysDown);
            }
        }

        // Passes unhandled messages back into the original Win32 procedure
        public IntPtr CallNextWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            return Win32RawInputMethods.CallWindowProc(originalWndProc, hWnd, msg, wParam, lParam);
        }

        public void Dispose(nint glfwHandle)
        {
            nint hWnd = Win32RawInputMethods.glfwGetWin32Window(glfwHandle);

            if (originalWndProc != IntPtr.Zero)
            {
                Win32RawInputMethods.SetWindowLongPtr(hWnd, Win32RawInputMethods.GWL_WNDPROC, originalWndProc);
                originalWndProc = IntPtr.Zero;
            }

            inputHookDelegate = null;
        }
    }
}

