using System;
using System.Windows;
using Rogero.ReactiveProperty;
using Serilog;
using Serilog.Core.Enrichers;

namespace Rogero.WpfNavigation.WpfTestApp
{
    public class RoutingTestWindowViewModel
    {
        public ReactiveProperty<RouterService> RouterService { get; } = new ReactiveProperty<RouterService>();
        public ReactiveProperty<string> SomeText { get; } = new ReactiveProperty<string>("blah blah face");

        public DelegateCommand OpenControl1CommandMain { get; set; }
        public DelegateCommand OpenControl2CommandMain { get; set; }
        public DelegateCommand OpenControl1CommandSecond { get; set; }
        public DelegateCommand OpenControl2CommandSecond { get; set; }

        private readonly RouteEntryRegistry _registry;
        private RouterService _routerService => RouterService.Value;

        public RoutingTestWindowViewModel()
        {
            _registry = new RouteEntryRegistry();
            var logger = new LoggerConfiguration()
                .Enrich.With(new PropertyEnricher("AppType", "UnitTest"))
                .WriteTo.Console().MinimumLevel.Verbose()
                .WriteTo.Seq("http://ws2012r2seq:5341", apiKey: "RrIxpZQpfUjcqk3NzTBY")
                .CreateLogger();

            RouterService.Value = new RouterService(_registry, new AlwaysGrantAccessRouteAuthorizationManager(), logger);

            OpenControl1CommandMain = new DelegateCommand(NavigateToOneMain);
            OpenControl2CommandMain = new DelegateCommand(NavigateToTwoMain);
            OpenControl1CommandSecond = new DelegateCommand(NavigateToOneSecond);
            OpenControl2CommandSecond = new DelegateCommand(NavigateToTwoSecond);
            Initialize();
        }

        private void NavigateToTwoMain()
        {
            var result = _routerService.RouteAsync("/control2", null, "MainViewport", null);
        }

        private void NavigateToOneMain()
        {
            var result = _routerService.RouteAsync("/control1", null, "MainViewport", null);
        }

        private void NavigateToTwoSecond()
        {
            var result = _routerService.RouteAsync("/control2", null, "SecondViewport", null);
        }

        private void NavigateToOneSecond()
        {
            var result = _routerService.RouteAsync("/control1", null, "SecondViewport", null);
        }

        private void Initialize()
        {
            var route1 = new RouteEntry("Route to Control1",
                                        "/control1",
                                        typeof(Control1),
                                        typeof(object),
                                        () => new Object());
            var route2 = new RouteEntry("Route to Control1",
                                        "/control2",
                                        typeof(Control2),
                                        typeof(object),
                                        () => new Object());

            _registry.RegisterRouteEntry(route1);
            _registry.RegisterRouteEntry(route2);
        }
    }

    internal class RouteEntry : IRouteEntry
    {
        public string Name { get; }
        public string Uri { get; }
        public Type ViewModelType { get; }
        public Type ViewType { get; }

        private readonly Func<object> _viewModelFunc;

        public RouteEntry(string name, string uri, Type viewType, Type viewModelType, Func<object> viewModelFunc)
        {
            _viewModelFunc = viewModelFunc;
            Name = name;
            Uri = uri;
            ViewType = viewType;
            ViewModelType = viewModelType;
        }

        public UIElement CreateView()
        {
            return (UIElement) Activator.CreateInstance(ViewType);
        }

        public object CreateViewModel()
        {
            return _viewModelFunc();
        }
    }
}