using System.Windows;
using System.Windows.Controls;
using Rogero.Options;

namespace Rogero.WpfNavigation.ViewportAdapters
{
    public class ContentControlViewportAdapter : ControlViewportAdapterBase
    {
        private readonly ContentControl _contentControl;

        public ContentControlViewportAdapter(ContentControl contentControl)
        {
            _contentControl = contentControl;
        }

        public override void AddControl(UIElement control)
        {
            _contentControl.Content = control;
        }

        public override Option<UIElement> ActiveControl => _contentControl.Content as UIElement;
    }

    public class WindowViewportAdapter : ControlViewportAdapterBase
    {
        private readonly Window _window;

        public WindowViewportAdapter(Window window)
        {
            _window = window;
        }


        public override void AddControl(UIElement control)
        {
            _window.Content = control;
            _window.Show();
        }

        public override Option<UIElement> ActiveControl { get; }
    }
}