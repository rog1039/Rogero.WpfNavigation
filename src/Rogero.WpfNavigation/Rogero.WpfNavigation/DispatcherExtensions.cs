using System;
using System.Windows.Threading;

namespace Rogero.WpfNavigation
{
    public static class DispatcherExtensions
    {
        public static void DelayInvoke(this Dispatcher dispatcher, TimeSpan delay, Action action)
        {
            var dt = new DispatcherTimer(DispatcherPriority.Normal, dispatcher)
            {
                Interval = delay
            };
            dt.Tick += (sender, args) =>
            {
                dt.Stop();
                action();
            };
            dt.Start();
        }

        /// <summary>
        /// Repeatedly invokes a function on the dispatcher until the function returns true, too much time has passed, or the function has been called too many times.
        /// </summary>
        /// <param name="dispatcher"></param>
        /// <param name="func">Function to perform. Function returns true if it should terminate, false will reschedcule the dispatcher to perform the function again.</param>
        /// <param name="delay"></param>
        /// <param name="maxTime"></param>
        /// <param name="maxIterations"></param>
        public static void InvokeUntil(this Dispatcher dispatcher, Func<DelayInvokeResult> func, TimeSpan delay, TimeSpan maxTime, int maxIterations)
        {
            var count = 0;
            var timeStarted = DateTime.UtcNow;
            TimeSpan Elapsed() => DateTime.UtcNow - timeStarted;
            bool ShouldStop() => Elapsed() > maxTime || count > maxIterations;

            var dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal, dispatcher) {Interval = delay};

            dispatcherTimer.Tick += (sender, args) =>
            {
                dispatcherTimer.Stop();
                var shouldQuit = func() == DelayInvokeResult.StopInvoking || ShouldStop();

                if (!shouldQuit)
                {
                    count++;
                    dispatcherTimer.Start();
                }
            };

            dispatcherTimer.Start();
        }
    }

    public enum DelayInvokeResult
    {
        KeepInvoking,
        StopInvoking
    }
}