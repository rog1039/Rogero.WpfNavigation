using Rogero.ReactiveProperty;

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
            RouterService.Value = new RouterService(_registry, new Logger());

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