using System;
using System.Windows;

namespace Rogero.WpfNavigation
{
    public class ViewVmPair
    {
        public Type ViewType { get; }
        public Func<object> VmFactory { get; set; }

        public ViewVmPair(Type viewType, Func<object> vmFactory)
        {
            ViewType = viewType;
            VmFactory = vmFactory;
        }

        public UIElement CreateView() => (UIElement)Activator.CreateInstance(ViewType);
        public object CreateViewModel() => VmFactory();
    }
}