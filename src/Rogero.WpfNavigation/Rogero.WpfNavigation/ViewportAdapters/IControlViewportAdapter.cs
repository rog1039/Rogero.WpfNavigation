using System.Collections.Generic;
using System.Windows;
using Rogero.Options;

namespace Rogero.WpfNavigation.ViewportAdapters
{
    public interface IControlViewportAdapter
    {
        void AddControl(UIElement control, RouteWorkflowTask routeWorkflowTask);
        Option<UIElement> ActiveControl { get; }
        Option<object> ActiveDataContext { get; }
        UIElement ViewportUIElement { get; set; }
        IList<RouteWorkflowTask> GetActiveRouteWorkflows();
        void Activate(RouteWorkflowTask activeRouteWorkflow);
        void CloseScreen(RouteWorkflowTask workflow);
    }

    public abstract class ControlViewportAdapterBase : IControlViewportAdapter
    {

        public Option<object> ActiveDataContext
        {
            get
            {
                if (ActiveControl?.Value is FrameworkElement frameworkElement) return frameworkElement.DataContext;

                return Option<object>.None;
            }
        }

        public UIElement ViewportUIElement { get; set; }
        public virtual Option<UIElement> ActiveControl { get; }
        
        public abstract IList<RouteWorkflowTask> GetActiveRouteWorkflows();
        public abstract void Activate(RouteWorkflowTask activeRouteWorkflow);
        public abstract void CloseScreen(RouteWorkflowTask workflow);

        public abstract void AddControl(UIElement control, RouteWorkflowTask routeWorkflowTask);
    }
}