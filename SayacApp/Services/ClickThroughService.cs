using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;

namespace SayacApp.Services;

/// <summary>
/// Makes a window click-through (mouse events pass to whatever is underneath) when the
/// mini overlay is locked. Windows: WS_EX_TRANSPARENT. macOS: NSWindow.ignoresMouseEvents.
/// Best-effort and never throws; on unsupported platforms it's a no-op.
/// </summary>
public static class ClickThroughService
{
    public static void Apply(Window window, bool clickThrough)
    {
        try
        {
            var handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            if (handle == IntPtr.Zero) return;

            if (OperatingSystem.IsWindows()) ApplyWindows(handle, clickThrough);
            else if (OperatingSystem.IsMacOS()) ApplyMac(handle, clickThrough);
        }
        catch
        {
            // Non-fatal: overlay just stays clickable.
        }
    }

    /// <summary>
    /// Hides a window from the Windows Alt-Tab switcher (and taskbar) by adding the
    /// WS_EX_TOOLWINDOW extended style. Used for the mini overlay so only the main
    /// window appears in Alt-Tab. Set once; no-op on macOS/Linux.
    /// </summary>
    public static void HideFromAltTab(Window window)
    {
        try
        {
            if (!OperatingSystem.IsWindows()) return;
            var handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            if (handle == IntPtr.Zero) return;

            var ex = (long)GetWindowLongPtr(handle, GWL_EXSTYLE);
            ex |= WS_EX_TOOLWINDOW;
            SetWindowLongPtr(handle, GWL_EXSTYLE, new IntPtr(ex));
        }
        catch
        {
            // Non-fatal: overlay just stays in Alt-Tab.
        }
    }

    // ---------- Windows ----------
    private const int GWL_EXSTYLE = -20;
    private const long WS_EX_TRANSPARENT = 0x00000020;
    private const long WS_EX_LAYERED = 0x00080000;
    private const long WS_EX_TOOLWINDOW = 0x00000080;

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
    private static extern IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

    private static void ApplyWindows(IntPtr hwnd, bool clickThrough)
    {
        var ex = (long)GetWindowLongPtr(hwnd, GWL_EXSTYLE);
        if (clickThrough) ex |= WS_EX_TRANSPARENT | WS_EX_LAYERED;
        else ex &= ~WS_EX_TRANSPARENT;
        SetWindowLongPtr(hwnd, GWL_EXSTYLE, new IntPtr(ex));
    }

    // ---------- macOS ----------
    // Avalonia's macOS platform handle is the NSWindow (AvnWindow) itself. We guard
    // every send with respondsToSelector: so an unknown selector can't raise an
    // Objective-C exception (those abort the process; try/catch can't catch them).
    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "sel_registerName")]
    private static extern IntPtr SelRegisterName(string name);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern byte MsgSendRespondsTo(IntPtr receiver, IntPtr selector, IntPtr argSelector);

    [DllImport("/usr/lib/libobjc.dylib", EntryPoint = "objc_msgSend")]
    private static extern void MsgSendSetBool(IntPtr receiver, IntPtr selector, byte arg);

    private static void ApplyMac(IntPtr nsWindow, bool clickThrough)
    {
        var setSel = SelRegisterName("setIgnoresMouseEvents:");
        var respondsSel = SelRegisterName("respondsToSelector:");
        if (MsgSendRespondsTo(nsWindow, respondsSel, setSel) != 0)
            MsgSendSetBool(nsWindow, setSel, clickThrough ? (byte)1 : (byte)0);
    }
}
