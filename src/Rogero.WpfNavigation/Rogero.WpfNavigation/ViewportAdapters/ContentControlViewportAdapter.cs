using System.Windows;
using System.Windows.Controls;

namespace Rogero.WpfNavigation
{
    public class ContentControlViewportAdapter : IControlViewportAdapter
    {
        private readonly ContentControl _contentControl;

        public ContentControlViewportAdapter(ContentControl contentControl)
        {
            _contentControl = contentControl;
        }

        public void AddControl(UIElement control)
        {
            _contentControl.Content = control;
        }
    }
}