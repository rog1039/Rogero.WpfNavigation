using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace Rogero.WpfNavigation
{
    public class TabControlViewportAdapter : IControlViewportAdapter
    {
        private readonly TabControl _tabControl;
        private readonly ObservableCollection<object> _views = new ObservableCollection<object>();

        public TabControlViewportAdapter(TabControl tabControl)
        {
            _tabControl = tabControl;
            _tabControl.ItemsSource = _views;
        }

        public void AddControl(UIElement control)
        {
            _views.Add(control);
        }
    }
}