#nullable enable
using System;
using System.Windows;

namespace Rogero.WpfNavigation.ViewportAdapters
{
    public static class WindowExtensions
    {
        /// <summary>
        /// Brings the specified window to the front. Returns the previous state of the window.
        /// </summary>
        /// <param name="window"></param>
        /// <param name="log"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static PreviousWindowActiveState BringWindowToFront(this Window window, Action<string>? log)
        {
            var previousWindowState = window.GetWindowState();

            switch (previousWindowState)
            {
                case PreviousWindowActiveState.WasInactive:
                {
                    log?.Invoke("Window was not activated, so activating window.");
                    window.Activate();
                    return PreviousWindowActiveState.WasInactive;
                }
                case PreviousWindowActiveState.WasMinimized:
                {
                    log?.Invoke("Window was minimized, so setting WindowState to Normal.");
                    window.WindowState = WindowState.Normal;
                    window.Activate();
                    return PreviousWindowActiveState.WasMinimized;
                }
                case PreviousWindowActiveState.WasActive:
                {
                    log?.Invoke("Window was active and not minimized.");
                    return PreviousWindowActiveState.WasActive;
                }
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// Returns if the specified window is Minimized, InActive, or Active
        /// </summary>
        /// <param name="window"></param>
        /// <returns></returns>
        public static PreviousWindowActiveState GetWindowState(this Window window)
        {
            var wasMinimized      = window.WindowState == WindowState.Minimized;
            if (wasMinimized) return PreviousWindowActiveState.WasMinimized;
            
            var wasWindowInactive = !window.IsActive;

            return wasWindowInactive 
                ? PreviousWindowActiveState.WasInactive
                : PreviousWindowActiveState.WasActive;
        }
    }
}