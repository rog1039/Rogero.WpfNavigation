using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using Optional;

namespace Rogero.WpfNavigation.ViewportAdapters
{
    public abstract class SingleContentViewportAdapter : ControlViewportAdapterBase
    {
        protected RouteWorkflowTask _routeWorkflowTask;

        public override IList<RouteWorkflowTask> GetActiveRouteWorkflows()
        {
            return new List<RouteWorkflowTask>(){_routeWorkflowTask};
        }
        
        public override Option<UIElement> ActiveControl =>
            _routeWorkflowTask?.View?.SomeNotNull() ?? Option.None<UIElement>();
    }
    public class ContentControlViewportAdapter : SingleContentViewportAdapter
    {
        private readonly ContentControl _contentControl;

        public ContentControlViewportAdapter(ContentControl contentControl)
        {
            _contentControl = contentControl;
            ViewportUIElement = contentControl;
        }

        public override void AddControl(UIElement control, RouteWorkflowTask routeWorkflowTask)
        {
            _contentControl.Content = control;
            _routeWorkflowTask = routeWorkflowTask;
        }

        public override void Activate(RouteWorkflowTask activeRouteWorkflow)
        {
            //There is nothing to do here since a ContentControl is showing its only content already.
        }

        public override void CloseScreen(RouteWorkflowTask workflow)
        {
            _contentControl.Content = null;
        }
    }

    public class WindowViewportAdapter : SingleContentViewportAdapter
    {
        private readonly Window _window;

        public WindowViewportAdapter(Window window)
        {
            _window = window;
            ViewportUIElement = window;
        }

        public override void Activate(RouteWorkflowTask activeRouteWorkflow)
        {
            _window.BringWindowToFront(null);
        }

        public override void AddControl(UIElement control, RouteWorkflowTask routeWorkflowTask)
        {
            _routeWorkflowTask = routeWorkflowTask;
            _window.Content = control;
            _window.Show();
        }

        public override void CloseScreen(RouteWorkflowTask workflow)
        {
            _window.Close();
        }
    }
}