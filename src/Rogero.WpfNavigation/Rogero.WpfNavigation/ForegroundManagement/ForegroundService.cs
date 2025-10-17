#nullable enable
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Serilog;

namespace Rogero.WpfNavigation.ForegroundManagement;

/// <summary>
/// A service designed to interact with the operating system's foreground functionality for WPF applications.
/// </summary>
/// <remarks>
/// The <see cref="ForegroundService"/> is a lightweight, production-ready service that safely manages functionality
/// such as bringing windows to the foreground, flashing windows, and determining OS-level foreground states.
/// Features:
/// - Provides a safe and reliable Bring-To-Front mechanism that adheres to Windows OS focus rules.
/// - Includes fallbacks such as a Topmost-toggle trick and Flashing for attention signaling.
/// - Optimized for use in response to user interaction (e.g., clicks, hotkeys, or tray icon events).
/// - Marshals to the UI thread when invoked from background threads.
/// Limitations:
/// - Avoids usage of `AttachThreadInput` due to its potential side effects.
/// - Application's window must be configured correctly in the WPF `Window` system.
/// Recommendations:
/// Use this service carefully, adhering to OS focus rules, and ensure user-triggered invocations
/// for optimal functionality.
/// For best results:
/// - Utilize dependency injection (e.g., Autofac) to register and resolve the service.
/// - Configure the main application window or specific windows to work with this service.
/// </remarks>
public sealed class ForegroundService : IForegroundService
{
  private Action<string>? _logMethod;

  public ForegroundService(Action<string>? logMethod = null)
  {
    
  }

  /// <summary>
  /// Attempts to bring the specified window to the front. If no window is specified, the application's main window is used.
  /// Implements several fallback strategies to respect Windows focus rules if the direct approach fails.
  /// </summary>
  /// <param name="window">The window to bring to the foreground. If null, the application's main window is targeted.</param>
  /// <param name="flashOnFail">Indicates whether to visually flash the window if it cannot be brought to the foreground.</param>
  /// <returns>True if the window is successfully brought to the foreground; otherwise, false.</returns>
  public bool TryBringToFront(Window? window = null, bool flashOnFail = true)
  {
    window ??= Application.Current?.MainWindow;
    if (window is null)
      return false;
    
    if (!window.Dispatcher.CheckAccess())
    {
      return window.Dispatcher.Invoke(() => TryBringToFront(window, flashOnFail));
    }

    var hwnd = EnsureHandle(window);
    if (hwnd == IntPtr.Zero)
      return false; // couldn't obtain a handle

    // 1) If minimized, restore first
    if (IsIconic(hwnd)) ShowWindow(hwnd, SW_RESTORE);

    // 2) Friendly path: Activate + Focus
    window.Activate();
    window.Focus();
    if (IsOsForegroundHwnd(hwnd))
      return true;

    // 3) Call SetForegroundWindow (allowed after user input / certain cases)
    if (SetForegroundWindow(hwnd) && IsOsForegroundHwnd(hwnd))
      return true;

    // 4) Topmost toggle trick: briefly hop above others without leaving it pinned
    bool wasTopmost = window.Topmost;
    try
    {
      window.Topmost = true;
      window.Activate();
    }
    finally
    {
      window.Topmost = wasTopmost;
    }

    if (IsOsForegroundHwnd(hwnd))
      return true;

    // 5) Respect focus rules and flash as a last resort
    if (flashOnFail)
      Flash(window);

    return false;
  }

  /// <summary>
  /// Flashes the specified window's taskbar icon to notify the user. If no window is provided, the main application window is used.
  /// </summary>
  /// <param name="window">The window whose taskbar icon should flash. If null, the current application's main window is used.</param>
  /// <param name="count">The number of flashes for the taskbar icon. Defaults to 5. If set to 0, the icon will continue flashing until the window is brought to the foreground.</param>
  public void Flash(Window? window = null, uint count = 5)
  {
    window ??= Application.Current?.MainWindow;
    if (window is null)
      return;

    if (!window.Dispatcher.CheckAccess())
    {
      window.Dispatcher.Invoke(() => Flash(window, count));
      return;
    }

    var hwnd = EnsureHandle(window);
    if (hwnd == IntPtr.Zero)
      return;

    // FLASHWINFO is the structure used by FlashWindowEx to describe what flashing behavior to apply.
    // cbSize:   size of the structure in bytes
    // hwnd:     target window handle
    // dwFlags:  bitmask controlling how to flash (caption, tray, continuous, etc.)
    // uCount:   number of flashes (0 = flash until window comes to foreground)
    // dwTimeout: interval in milliseconds between flashes (0 = system default)
    var fwi = new FLASHWINFO
    {
      cbSize    = (uint)Marshal.SizeOf<FLASHWINFO>(),
      hwnd      = hwnd,
      dwFlags   = FLASHW_ALL | FLASHW_TIMERNOFG, // flash taskbar icon until user foregrounds
      uCount    = count,
      dwTimeout = 0
    };
    FlashWindowEx(ref fwi);
  }

  /// <summary>
  /// Determines whether the specified window is currently the OS foreground window.
  /// </summary>
  /// <param name="window">The window instance to check. If not provided, the application's main window is used.</param>
  /// <returns>True if the specified window is the OS foreground window; otherwise, false.</returns>
  public bool IsOsForeground(Window? window = null)
  {
    window ??= Application.Current?.MainWindow;
    if (window is null)
      return false;

    if (!window.Dispatcher.CheckAccess())
    {
      return window.Dispatcher.Invoke(() => IsOsForeground(window));
    }

    var hwnd = EnsureHandle(window);
    return hwnd != IntPtr.Zero && IsOsForegroundHwnd(hwnd);
  }

  /// <summary>
  /// Ensures that the specified window has a valid HWND handle. If the handle does not exist, it forces its creation.
  /// </summary>
  /// <param name="window">The window instance for which to ensure an HWND handle.</param>
  /// <returns>A valid HWND handle of the specified window, or IntPtr.Zero if the handle could not be created.</returns>
  private static IntPtr EnsureHandle(Window window)
  {
    var helper = new WindowInteropHelper(window);
    if (helper.Handle != IntPtr.Zero)
      return helper.Handle;

    // Hook and rerun once an HWND exists
    IntPtr result = IntPtr.Zero;

    void OnSourceInitialized(object? s, EventArgs e)
    {
      window.SourceInitialized -= OnSourceInitialized;
      result                   =  new WindowInteropHelper(window).Handle;
    }

    window.SourceInitialized += OnSourceInitialized;

    // Force creation of the handle if possible
    window.Show();
    return new WindowInteropHelper(window).Handle;
  }

  /// <summary>
  /// Determines if the specified window handle is the OS foreground window.
  /// </summary>
  /// <param name="hwnd">The handle of the window to check.</param>
  /// <returns>True if the specified window handle is the foreground window; otherwise, false.</returns>
  private static bool IsOsForegroundHwnd(IntPtr hwnd)
  {
    return hwnd != IntPtr.Zero && hwnd == GetForegroundWindow();
  }

  /// <summary>
  /// Retrieves the handle (HWND) of the window currently in the foreground.
  /// </summary>
  /// <returns>Handle to the foreground window, or IntPtr.Zero if there is no foreground window.</returns>
  [DllImport("user32.dll")]
  private static extern IntPtr GetForegroundWindow();

  /// <summary>
  /// Attempts to bring the specified window to the foreground.
  /// </summary>
  /// <param name="hWnd">A handle to the window to be brought to the foreground.</param>
  /// <returns>Returns true if the window was successfully brought to the foreground; otherwise, false.</returns>
  [DllImport("user32.dll")]
  private static extern bool SetForegroundWindow(IntPtr hWnd);

  /// <summary>
  /// Determines whether the specified window is minimized (iconic).
  /// </summary>
  /// <param name="hWnd">A handle to the window to be tested.</param>
  /// <returns>True if the window is minimized; otherwise, false.</returns>
  [DllImport("user32.dll")]
  private static extern bool IsIconic(IntPtr hWnd);

  /// <summary>
  /// Changes the visibility or state of the specified window based on the provided command.
  /// </summary>
  /// <param name="hWnd">A handle to the window whose visibility or state is to be changed.</param>
  /// <param name="nCmdShow">Specifies the command to be applied to the window. Common values include SW_SHOWNORMAL, SW_MINIMIZE, SW_RESTORE, etc.</param>
  /// <returns>Returns true if successful, otherwise false.</returns>
  [DllImport("user32.dll")]
  private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

  /// <summary>
  /// Represents a constant used with the `ShowWindow` function to restore a window
  /// that has been minimized to its normal state. If the window is iconic (minimized),
  /// this constant requests the window to be restored.
  /// </summary>
  private const int SW_RESTORE = 9;
  //Other SW_ constants defined below:
  private const int SW_HIDE            = 0;  // Hides the window and activates another window.
  private const int SW_SHOWNORMAL      = 1;  // Activates and displays a window. If minimized/maximized, restores to original size/position.
  private const int SW_NORMAL          = 1;  // Same as SW_SHOWNORMAL.
  private const int SW_SHOWMINIMIZED   = 2;  // Activates the window and displays it as a minimized window.
  private const int SW_SHOWMAXIMIZED   = 3;  // Activates the window and displays it as a maximized window.
  private const int SW_MAXIMIZE        = 3;  // Same as SW_SHOWMAXIMIZED.
  private const int SW_SHOWNOACTIVATE  = 4;  // Displays a window in its most recent size/position. Does not activate.
  private const int SW_SHOW            = 5;  // Activates the window and displays it in its current size/position.
  private const int SW_MINIMIZE        = 6;  // Minimizes the specified window and activates the next top-level window.
  private const int SW_SHOWMINNOACTIVE = 7;  // Displays the window as minimized. Does not activate.
  private const int SW_SHOWNA          = 8;  // Displays the window in its current size/position. Does not activate.
  // SW_RESTORE = 9 defined above
  private const int SW_SHOWDEFAULT     = 10; // Sets the show state based on the STARTUPINFO passed by the program that launched the app.
  private const int SW_FORCEMINIMIZE   = 11; // Minimizes a window, even if the thread owning the window is not responding. Use for hung apps.

  
  /// <summary>
  /// Represents a structure used by the FlashWindowEx API to specify the behavior of window flashing.
  /// It defines parameters such as the window handle, flash options, number of flashes, and the flash rate.
  /// </summary>
  [StructLayout(LayoutKind.Sequential)]
  private struct FLASHWINFO
  {
    public uint   cbSize;    // Size of this structure in bytes
    public IntPtr hwnd;      // Window handle to flash
    public uint   dwFlags;   // Flash options (caption, tray, continuous, etc.)
    public uint   uCount;    // Number of flashes (0 = flash until foreground)
    public uint   dwTimeout; // Rate of flashes in ms (0 = default system rate)
  }

  /// <summary>
  /// Causes a window's taskbar button or title bar to flash, indicating it demands user attention.
  /// </summary>
  /// <param name="pwfi">A reference to a FLASHWINFO structure that specifies the flash operation.</param>
  /// <returns>Returns true if the flashing was successful; otherwise, false.</returns>
  [DllImport("user32.dll")]
  private static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

  /// <summary>
  /// Represents a constant used with the flash window mechanism to specify
  /// that the title bar (caption) of a window should be flashed.
  /// Typically used in scenarios where attention needs to be drawn to the window's title bar.
  /// </summary>
  private const uint FLASHW_CAPTION   = 0x00000001; // Flash the window caption/title bar

  /// <summary>
  /// Represents a constant used with the `FlashWindowEx` function to specify that only the taskbar
  /// button should be flashed. This is used to alert the user without flashing the window's title bar.
  /// </summary>
  private const uint FLASHW_TRAY      = 0x00000002; // Flash the taskbar button

  /// <summary>
  /// Represents a constant used to specify that both the window's title bar
  /// and the taskbar button should be flashed. Typically used in scenarios
  /// where the user's attention is required for a window.
  /// </summary>
  private const uint FLASHW_ALL = FLASHW_CAPTION | FLASHW_TRAY; // Flash both caption and taskbar

  /// <summary>
  /// Represents a constant used to specify that the window caption and taskbar button should flash continuously
  /// until the flashing is manually stopped. This flag can be used with certain window flashing methods.
  /// </summary>
  private const uint FLASHW_TIMER     = 0x00000004; // Flash continuously, until stopped

  /// <summary>
  /// Represents a constant used with the `FlashWindowEx` function to specify that
  /// the flashing behavior should continue even if the window does not have
  /// focus (does not come to the foreground). This constant is used in combination
  /// with other flags to control the flashing behavior of the taskbar icon or
  /// window caption.
  /// </summary>
  private const uint FLASHW_TIMERNOFG = 0x0000000C; // Flash until window comes to foreground (TIMER | NOFG)

  /// <summary>
  /// Represents a constant used with the `FlashWindowEx` function to stop any flashing
  /// of a window. When this constant is used, no flash will occur, and
  /// any ongoing flashing for the window will be halted.
  /// </summary>
  private const uint FLASHW_STOP      = 0x00000000; // Stop flashing  
}