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
}