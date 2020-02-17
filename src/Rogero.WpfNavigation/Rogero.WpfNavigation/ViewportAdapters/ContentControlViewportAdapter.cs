using System.Windows;
using System.Windows.Controls;
using Rogero.Options;

namespace Rogero.WpfNavigation.ViewportAdapters
{
    public class ContentControlViewportAdapter : ControlViewportAdapterBase
    {
        private readonly ContentControl _contentControl;
        private RouteWorkflowTask _routeWorkflowTask;

        public ContentControlViewportAdapter(ContentControl contentControl)
        {
            _contentControl = contentControl;
            AssociatedUIElement = contentControl;
        }

        public override void AddControl(UIElement control, RouteWorkflowTask routeWorkflowTask)
        {
            _contentControl.Content = control;
            _routeWorkflowTask = routeWorkflowTask;
        }

        public override Option<UIElement> ActiveControl => _contentControl.Content as UIElement;
        public override UIElement AssociatedUIElement { get; set; }
    }

    public class WindowViewportAdapter : ControlViewportAdapterBase
    {
        private readonly Window _window;

        public WindowViewportAdapter(Window window)
        {
            _window = window;
            AssociatedUIElement = window;
        }


        public override void AddControl(UIElement control, RouteWorkflowTask routeWorkflowTask)
        {
            _window.Content = control;
            _window.Show();
        }

        public override Option<UIElement> ActiveControl { get; }
        public override UIElement AssociatedUIElement { get; set; }
    }
}