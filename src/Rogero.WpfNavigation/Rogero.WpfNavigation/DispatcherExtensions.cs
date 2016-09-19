using System;
using System.Windows.Threading;

namespace Rogero.WpfNavigation
{
    public static class DispatcherExtensions
    {
        public static void DelayInvoke(this Dispatcher dispatcher, TimeSpan delay, Action action)
        {
            var dt = new DispatcherTimer(DispatcherPriority.Normal, dispatcher);
            dt.Interval = delay;
            dt.Tick += (sender, args) =>
            {
                dt.Stop();
                action();
            };
            dt.Start();
        }
    }
}