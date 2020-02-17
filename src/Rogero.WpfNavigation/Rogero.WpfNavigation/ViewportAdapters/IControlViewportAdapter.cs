using System.Windows;
using Rogero.Options;

namespace Rogero.WpfNavigation.ViewportAdapters
{
    public interface IControlViewportAdapter
    {
        void AddControl(UIElement control, RouteWorkflowTask routeWorkflowTask);
        Option<UIElement> ActiveControl { get; }
        Option<object> ActiveDataContext { get; }
        UIElement AssociatedUIElement { get; set; }
    }

    public abstract class ControlViewportAdapterBase : IControlViewportAdapter
    {
        public abstract void AddControl(UIElement control, RouteWorkflowTask routeWorkflowTask);
        public abstract Option<UIElement> ActiveControl { get; }

        public Option<object> ActiveDataContext
        {
            get
            {
                if (ActiveControl?.Value is FrameworkElement frameworkElement) return frameworkElement.DataContext;

                return Option<object>.None;
            }
        }

        public abstract UIElement AssociatedUIElement { get; set; }
    }
}