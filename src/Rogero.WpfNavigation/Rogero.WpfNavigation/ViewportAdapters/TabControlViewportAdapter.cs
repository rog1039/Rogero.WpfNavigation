using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Optional;

namespace Rogero.WpfNavigation.ViewportAdapters
{
    public class TabControlViewportAdapter : ControlViewportAdapterBase
    {
        private readonly TabControl _tabControl;
        private readonly ObservableCollection<object> _views = new ObservableCollection<object>();
        private readonly ObservableCollection<RouteWorkflowTask> _routeWorkflowTasks = new ObservableCollection<RouteWorkflowTask>();

        public TabControlViewportAdapter(TabControl tabControl)
        {
            ViewportUIElement = tabControl;
            _tabControl = tabControl;
            _tabControl.ItemsSource = _views;
        }

        public override void Activate(RouteWorkflowTask activeRouteWorkflow)
        {
            _tabControl.SelectedItem = activeRouteWorkflow.Controller;
        }

        public override void AddControl(UIElement control, RouteWorkflowTask routeWorkflowTask)
        {
            _views.Add(control);
            _routeWorkflowTasks.Add(routeWorkflowTask);
        }

        public override Option<UIElement> ActiveControl
        {
            get
            {
                var selectedItem = _tabControl.SelectedItem;
                if (selectedItem is null) return Option.None<UIElement>();
                if (selectedItem is UIElement uiElement) return uiElement.SomeNotNull();
                throw new InvalidOperationException("Never expected GetActiveControl to return an item outside the UIElement hierarchy.");
            }
        }

        public override IList<RouteWorkflowTask> GetActiveRouteWorkflows()
        {
            return _routeWorkflowTasks.ToList();
        }

        public override void CloseScreen(RouteWorkflowTask workflow)
        {
            throw new NotImplementedException();
        }
    }
    
}