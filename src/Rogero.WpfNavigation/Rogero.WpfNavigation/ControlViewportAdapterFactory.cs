using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Rogero.Options;
using Rogero.WpfNavigation.ViewportAdapters;

namespace Rogero.WpfNavigation
{
    public class ControlViewportAdapterFactory
    {
        private static readonly IDictionary<Type, Type> FrameworkElementToViewportAdapterMap = new Dictionary<Type, Type>()
        {
            { typeof(ContentControl), typeof(ContentControlViewportAdapter) }
        };

        public static void AddViewportControlAdapter(Type frameworkElement, Type viewportAdapter)
        {
            FrameworkElementToViewportAdapterMap.Add(frameworkElement, viewportAdapter);
        }

        public static Option<IControlViewportAdapter> GetControlViewportAdapter(FrameworkElement control)
        {
            var adapterType = FrameworkElementToViewportAdapterMap.TryGetValue(control.GetType());
            if (adapterType.HasNoValue) return Option<IControlViewportAdapter>.None;

            var instance = (IControlViewportAdapter)Activator.CreateInstance(adapterType.Value, control);
            return instance.ToOption();
        }
    }
}