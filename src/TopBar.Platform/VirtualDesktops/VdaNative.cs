using System.Runtime.InteropServices;

namespace TopBar.Platform.VirtualDesktops;

/// <summary>
/// P/Invoke for VirtualDesktopAccessor.dll (https://github.com/Ciantic/VirtualDesktopAccessor, "Windows 11" build).
/// All functions return -1 on error. Signatures verified against the README of the release used
/// (GetDesktopName: (i32 number, u8* out_utf8, usize len) -> i32; RegisterPostMessageHook: (HWND, u32 message_offset) -> i32).
/// </summary>
internal static class VdaNative
{
    public const string DllName = "VirtualDesktopAccessor.dll";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetDesktopCount();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetCurrentDesktopNumber();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GoToDesktopNumber(int desktopNumber);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int GetDesktopName(int desktopNumber, byte[] outUtf8, nuint length);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int RegisterPostMessageHook(nint listenerHwnd, uint messageOffset);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int UnregisterPostMessageHook(nint listenerHwnd);
}
