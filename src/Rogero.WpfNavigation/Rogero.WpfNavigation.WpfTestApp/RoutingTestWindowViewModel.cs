using Rogero.ReactiveProperty;
using Serilog;
using Serilog.Core.Enrichers;
using Serilog.Events;

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

        private readonly RouteRegistry _registry;
        private RouterService _routerService => RouterService.Value;

        public RoutingTestWindowViewModel()
        {
            _registry = new RouteRegistry();
            var logger = new LoggerConfiguration()
                .Enrich.With(new PropertyEnricher("AppType", "UnitTest"))
                .WriteTo.Console().MinimumLevel.Verbose()
                .WriteTo.Seq("http://ws2012r2seq:5341", apiKey: "RrIxpZQpfUjcqk3NzTBY")
                .CreateLogger();

            RouterService.Value = new RouterService(_registry, logger);

            OpenControl1CommandMain = new DelegateCommand(NavigateToOneMain);
            OpenControl2CommandMain = new DelegateCommand(NavigateToTwoMain);
            OpenControl1CommandSecond = new DelegateCommand(NavigateToOneSecond);
            OpenControl2CommandSecond = new DelegateCommand(NavigateToTwoSecond);
            Initialize();
        }

        private void NavigateToTwoMain()
        {
            var result = _routerService.RouteAsync("control2", null, "MainViewport");
        }

        private void NavigateToOneMain()
        {
            var result = _routerService.RouteAsync("control1", null, "MainViewport");
        }

        private void NavigateToTwoSecond()
        {
            var result = _routerService.RouteAsync("control2", null, "SecondViewport");
        }

        private void NavigateToOneSecond()
        {
            var result = _routerService.RouteAsync("control1", null, "SecondViewport");
        }

        private void Initialize()
        {
            _registry.Register<Control1>("control1", () => new object());
            _registry.Register<Control2>("control2", () => new object());
        }
    }
}