#nullable enable
using System.Windows;

namespace Rogero.WpfNavigation.ForegroundManagement;

/// <summary>
/// Defines functionality for handling OS-level foreground behavior in WPF applications.
/// Provides methods to bring windows to the foreground, flash taskbar buttons to request user attention,
/// and check if a window is the current OS foreground window.
///
/// A small, production-ready foreground service for WPF
/// - Provides safe BringToFront with fallbacks that respect Windows focus rules
/// - Works best when called in response to user input (click/hotkey/tray)
///
/// Usage (plain):
///   IForegroundService fg = new ForegroundService();
///   fg.TryBringToFront(); // brings Application.Current.MainWindow to front
///
/// Usage (Autofac):
///   builder.RegisterType<ForegroundService>().As<IForegroundService>().SingleInstance();
///   // resolve and call TryBringToFront() where needed
///
/// Notes:
/// - We avoid AttachThreadInput due to side effects; we use Topmost-toggle and Flash fallbacks
/// - IsActive is not the same as being OS foreground; we check GetForegroundWindow
/// - If you call this from a background thread, we marshal to the UI Dispatcher
/// </summary>
public interface IForegroundService
{
  /// <summary>
  /// Attempts to bring the window (or the MainWindow if null) to the OS foreground, restoring if minimized.
  /// Returns true if the window ends up as the OS foreground window. If it fails and flashOnFail is true, flashes the taskbar.
  /// </summary>
  bool TryBringToFront(Window? window = null, bool flashOnFail = true);

  /// <summary>
  /// Flashes the taskbar button to request user attention. Returns immediately.
  /// </summary>
  void Flash(Window? window = null, uint count = 5);

  /// <summary>
  /// Checks if the given window (or MainWindow if null) is the actual OS foreground window.
  /// </summary>
  bool IsOsForeground(Window? window = null);
}