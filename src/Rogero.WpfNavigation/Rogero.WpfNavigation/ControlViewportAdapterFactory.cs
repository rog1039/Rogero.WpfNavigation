using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Optional;
using Optional.Collections;
using Optional.Linq;
using Rogero.WpfNavigation.ExtensionMethods;
using Rogero.WpfNavigation.ViewportAdapters;

namespace Rogero.WpfNavigation
{
    public class ControlViewportAdapterFactory
    {
        private static readonly IDictionary<Type, Type> FrameworkElementTypeToViewportAdapterTypeMap = new Dictionary<Type, Type>()
        {
            { typeof(ContentControl), typeof(ContentControlViewportAdapter) },
        };

        public static void AddViewportControlAdapter(Type frameworkElement, Type viewportAdapter)
        {
            FrameworkElementTypeToViewportAdapterTypeMap.Add(frameworkElement, viewportAdapter);
        }

        public static Option<IControlViewportAdapter> GetControlViewportAdapter(FrameworkElement control)
        {
            var controlViewportAdapterTypeOption = FrameworkElementTypeToViewportAdapterTypeMap.GetValueOrNone(control.GetType());
            return controlViewportAdapterTypeOption.Select(controlViewportAdapterType =>
            {
                var controlViewportAdapter = (IControlViewportAdapter) Activator.CreateInstance(controlViewportAdapterType, control);
                return controlViewportAdapter;
            });
        }
    }
}