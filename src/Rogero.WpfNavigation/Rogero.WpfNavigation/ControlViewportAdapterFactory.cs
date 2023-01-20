using System.Windows;
using System.Windows.Controls;
using Optional;
using Optional.Collections;
using Optional.Linq;
using Rogero.WpfNavigation.ViewportAdapters;

namespace Rogero.WpfNavigation;

public class ControlViewportAdapterFactory
{
   private static readonly IDictionary<Type, Type> FrameworkElementTypeToViewportAdapterTypeMap = new Dictionary<Type, Type>()
   {
      { typeof(ContentControl), typeof(ContentControlViewportAdapter) },
   };

   public static void AddViewportControlAdapter(Type frameworkElement, Type viewportAdapter)
   {
      var existingAdapter = FrameworkElementTypeToViewportAdapterTypeMap.GetValueOrNone(frameworkElement);
      existingAdapter.Match(
         some =>
         {
            if (some != viewportAdapter)
               throw new Exception($"Trying to register new adapter {viewportAdapter} but already registered" +
                                   $"with {some}");
         },
         () => { FrameworkElementTypeToViewportAdapterTypeMap.Add(frameworkElement, viewportAdapter); });
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