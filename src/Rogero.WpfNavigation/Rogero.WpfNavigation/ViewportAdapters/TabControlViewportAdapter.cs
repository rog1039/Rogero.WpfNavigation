using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using Rogero.Options;

namespace Rogero.WpfNavigation.ViewportAdapters
{
    public class TabControlViewportAdapter : ControlViewportAdapterBase
    {
        private readonly TabControl _tabControl;
        private readonly ObservableCollection<object> _views = new ObservableCollection<object>();

        public TabControlViewportAdapter(TabControl tabControl)
        {
            _tabControl = tabControl;
            _tabControl.ItemsSource = _views;
        }

        public override void AddControl(UIElement control)
        {
            _views.Add(control);
        }

        public override Option<UIElement> ActiveControl
        {
            get
            {
                var selectedItem = _tabControl.SelectedItem;
                if (selectedItem is null) return Option<UIElement>.None;
                if (selectedItem is UIElement uiElement) return uiElement;
                throw new InvalidOperationException("Never expected GetActiveControl to return an item outside the UIElement hierarchy.");
            }
        }
    }
}