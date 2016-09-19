using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Rogero.Options;

namespace Rogero.WpfNavigation
{
    public class ControlViewportAdapterFactory
    {
        private static IDictionary<Type, Type> _frameworkElementToViewportAdapterMap = new Dictionary<Type, Type>()
        {
            { typeof(ContentControl), typeof(ContentControlViewportAdapter) }
        };
        public static Option<IControlViewportAdapter> GetControlViewportAdapter(FrameworkElement control)
        {
            var adapterType = _frameworkElementToViewportAdapterMap.TryGetValue(control.GetType());
            if (adapterType.HasNoValue) return Option<IControlViewportAdapter>.None;

            var instance = (IControlViewportAdapter)Activator.CreateInstance(adapterType.Value, control);
            return instance.ToOption();
        }
    }
}