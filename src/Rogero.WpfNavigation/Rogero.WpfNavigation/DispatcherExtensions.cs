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
        /// Repeatedly invokes a function on the dispatcher until the function returns 'StopInvoking', too much time has passed, or the function has been called too many times.
        /// </summary>
        /// <param name="dispatcher"></param>
        /// <param name="function">Function to perform. Function returns StopInvoking if it should terminate, false will reschedule the dispatcher to perform the function again.</param>
        /// <param name="timeBetweenIterations"></param>
        /// <param name="maxTimeSpan"></param>
        /// <param name="maxIterations"></param>
        public static void InvokeUntil(
            this Dispatcher dispatcher,
            Func<DelayInvokeResult> function,
            TimeSpan timeBetweenIterations,
            TimeSpan maxTimeSpan,
            int maxIterations)
        {
            var iterationCount = 0;
            var timeStarted = DateTime.UtcNow;

            TimeSpan Elapsed() => DateTime.UtcNow - timeStarted;
            bool PastStopTime() => Elapsed() > maxTimeSpan;
            bool PastIterationLimit() => iterationCount > maxIterations;
            bool ShouldStop() => PastStopTime() || PastIterationLimit();

            var dispatcherTimer = new DispatcherTimer(DispatcherPriority.Normal, dispatcher) {Interval = timeBetweenIterations};

            dispatcherTimer.Tick += (sender, args) =>
            {
                dispatcherTimer.Stop();
                var functionSignaledQuit = function() == DelayInvokeResult.StopInvoking;
                var shouldQuit = functionSignaledQuit || ShouldStop();

                if (shouldQuit) return;
                
                iterationCount++;
                dispatcherTimer.Start();
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