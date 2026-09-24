using System.Runtime.InteropServices;

namespace ProjectOdyssey.Input.Native
{
    // Matches the Win32 RAWINPUTHEADER structure used by the Raw Input API
    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUTHEADER
    {
        public int Type;
        public int Size;
        public nint Device;
        public nint WParam;
    }

    // Contains low-level keyboard input data from the Windows Raw Input system
    [StructLayout(LayoutKind.Sequential)]
    public struct RAWKEYBOARD
    {
        public ushort MakeCode;
        public ushort Flags;
        public ushort Reserved;
        public ushort VKey;
        public uint Message;
        public uint ExtraInformation;
    }

    // Complete raw input packet combining header and keyboard data
    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUT
    {
        public RAWINPUTHEADER Header;
        public RAWKEYBOARD Keyboard;
    }

    // Used to register the application to receive raw keyboard input from Windows
    [StructLayout(LayoutKind.Sequential)]
    public struct RAWINPUTDEVICE
    {
        public ushort UsagePage;
        public ushort Usage;
        public uint Flags;
        public nint HwndTarget;
    }
}