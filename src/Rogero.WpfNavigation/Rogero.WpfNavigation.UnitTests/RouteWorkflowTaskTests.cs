using System;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Moq;
using Rogero.AutoFixture.Helpers;
using Optional;
using Rogero.WpfNavigation.ViewportAdapters;
using Xunit;
using Serilog;
using Serilog.Core;
using Shouldly;

namespace Rogero.WpfNavigation.UnitTests
{
    public class RouteWorkflowTaskTests : AutoFixtureBase
    {
        private Logger _logger;
        private IRouteEntry _routeEntryBase;
        private ContentControl _contentControl;
        private ContentControlViewportAdapter _contentControlViewportAdapter;
        private RouteRequest _routeRequest;
        private IViewModelNoInit _viewModelNoInit;
        private IViewModelInit0ParamsReturnsVoid _viewModelInit0ParamsReturnsVoid;
        private IViewModelInit0ParamsReturnsTask _viewModelInit0ParamsReturnsTask;
        private IViewModelInit1ParamsReturnsTask _viewModelInit1ParamsReturnsTask;
        private IViewAwareVm _viewAwareViewModel;

        public RouteWorkflowTaskTests()
        {
            _logger        = new LoggerConfiguration().WriteTo.Console().CreateLogger();
            InternalLogger.LoggerInstance = _logger;

            _viewModelNoInit = _fixture.GetMock<IViewModelNoInit>().Object;
            _viewModelInit0ParamsReturnsVoid = _fixture.GetMock<IViewModelInit0ParamsReturnsVoid>().Object;
            _viewModelInit0ParamsReturnsTask = _fixture.GetMock<IViewModelInit0ParamsReturnsTask>().Object;
            _viewModelInit1ParamsReturnsTask = _fixture.GetMock<IViewModelInit1ParamsReturnsTask>().Object;
            _viewAwareViewModel = _fixture.GetMock<IViewAwareVm>().Object;
        }

        private async Task<RouteResult> RunTest(TestParameters parameters)
        {
            ConfigureTest(parameters);
            return await RunRouteWorkflow(parameters);
        }

        private void ConfigureTest(TestParameters parameters)
        {
            ConfigureAuthorization(parameters);
            ConfigureICanDeactivate(parameters);
            ConfigureNewViewModel(parameters);

            _contentControl = new ContentControl();
            _contentControlViewportAdapter = new ContentControlViewportAdapter(_contentControl);


            _fixture.GetMock<IRouteEntryRegistry>()
                .Setup(z => z.GetRouteEntry(It.IsAny<string>()))
                .Returns(() => ((IRouteEntry)_routeEntryBase).SomeNotNull());
            _fixture.GetMock<IRouterService>()
                .Setup(z => z.GetControlViewportAdapter(It.IsAny<ViewportOptions>()))
                .Returns(() => _contentControlViewportAdapter.Some<IControlViewportAdapter>());

        }

        private void ConfigureAuthorization(TestParameters parameters)
        {
            _fixture.GetMock<IRouteAuthorizationManager>()
                .Setup(z => z.CheckAuthorization(It.IsAny<RoutingContext>()))
                .Returns(async () => parameters.IsAuthorized
                             ? RouteAuthorizationResult.Granted
                             : RouteAuthorizationResult.Denied);
        }

        private void ConfigureICanDeactivate(TestParameters parameters)
        {
            _fixture.GetMock<IRouterService>()
                .Setup(z => z.GetActiveDataContext(It.IsAny<ViewportOptions>()))
                .Returns(() => Option.Some<object>(new ICanDeactivateMock(parameters.CanDeactivate)));
        }

        private void ConfigureNewViewModel(TestParameters parameters)
        {
            _routeEntryBase = new RouteEntryMock(new Control(), parameters.NewViewModel);
        }

        private async Task<RouteResult> RunRouteWorkflow(TestParameters parameters)
        {
            var sut = new RouteWorkflowTask(parameters.RouteRequest,
                                            _fixture.GetMock<IRouteEntryRegistry>().Object,
                                            _fixture.GetMock<IRouteAuthorizationManager>().Object,
                                            _fixture.GetMock<IRouterService>().Object,
                                            _logger);

            var routeResult = await sut.GoAsync();
            return routeResult;
        }

        public class ICanDeactivateMock : ICanDeactivate
        {
            private readonly bool _canDeactivate;

            public ICanDeactivateMock(bool canDeactivate)
            {
                _canDeactivate = canDeactivate;
            }

            public async Task<bool> CanDeactivate(string uri, object initData)
            {
                return _canDeactivate;
            }
        }

        public class RouteEntryMock : IRouteEntry
        {
            public string Name { get; }
            public string Uri { get; }
            public Type ViewModelType { get; }
            public Type ViewType { get; }

            private readonly UIElement _view;
            private readonly object _viewModel;

            public RouteEntryMock(UIElement view, object viewModel)
            {
                _view = view;
                _viewModel = viewModel;
            }

            public UIElement CreateView()
            {
                return _view;
            }

            public object CreateViewModel()
            {
                return _viewModel;
            }
        }

        public interface IViewModelNoInit { }

        public interface IViewModelInit0ParamsReturnsVoid
        {
            void Init();
        }
        public interface IViewModelInit0ParamsReturnsTask
        {
            Task Init();
        }
        public interface IViewModelInit1ParamsReturnsTask
        {
            Task Init(decimal d);
        }
        public interface IViewAwareVm : IViewAware
        {
            
        }

        [WpfFact()]
        [Trait("Category", "Instant")]
        public async Task SuccessPath()
        {
            var parameters = new TestParameters(canDeactivate: true, isAuthorized: true, newViewModel: _viewModelNoInit);
            var routeResult = await RunTest(parameters);
            routeResult.Success.ShouldBe(true);
            routeResult.StatusCode.ShouldBe(RouteResultStatusCode.OK);
        }

        [WpfFact()]
        [Trait("Category", "Instant")]
        public async Task NotAuthorized()
        {
            var parameters = new TestParameters(canDeactivate: true, isAuthorized: false, newViewModel: _viewModelNoInit);
            var routeResult = await RunTest(parameters);
            routeResult.Success.ShouldBe(false);
            routeResult.StatusCode.ShouldBe(RouteResultStatusCode.Unauthorized);
        }

        [WpfFact()]
        [Trait("Category", "Instant")]
        public async Task CanDeactiveIsFalse()
        {
            var parameters = new TestParameters(canDeactivate: false, isAuthorized: true, newViewModel: _viewModelNoInit);
            var routeResult = await RunTest(parameters);
            routeResult.Success.ShouldBe(false);
            routeResult.StatusCode.ShouldBe(RouteResultStatusCode.CanDeactiveFailed);
        }
        
        [WpfFact()]
        [Trait("Category", "Instant")]
        public async Task Init0ParamsReturnsVoid()
        {
            _fixture.GetMock<IViewModelInit0ParamsReturnsVoid>()
                .Setup(z => z.Init())
                .Verifiable();

            var parameters = new TestParameters(canDeactivate: true, isAuthorized: true, newViewModel: _viewModelInit0ParamsReturnsVoid);
            var routeResult = await RunTest(parameters);
            routeResult.Success.ShouldBe(true);
            routeResult.StatusCode.ShouldBe(RouteResultStatusCode.OK);

            _fixture.GetMock<IViewModelInit0ParamsReturnsVoid>()
                .Verify();
        }

        [WpfFact()]
        [Trait("Category", "Instant")]
        public async Task Init0ParamsReturnsTask()
        {
            _fixture.GetMock<IViewModelInit0ParamsReturnsTask>()
                .Setup(z => z.Init())
                .Returns(() => Task.CompletedTask);

            var parameters = new TestParameters(canDeactivate: true, isAuthorized: true, newViewModel: _viewModelInit0ParamsReturnsTask);
            var routeResult = await RunTest(parameters);
            routeResult.Success.ShouldBe(true);
            routeResult.StatusCode.ShouldBe(RouteResultStatusCode.OK);

            _fixture.GetMock<IViewModelInit0ParamsReturnsTask>()
                .Verify();
        }

        [WpfFact()]
        [Trait("Category", "Instant")]
        public async Task Init1ParamsReturnsVoid()
        {
            _fixture.GetMock<IViewModelInit1ParamsReturnsTask>()
                .Setup(z => z.Init(It.IsAny<decimal>()))
                .Returns(() => Task.CompletedTask);

            var routeRequest = new RouteRequest("", (decimal)25,ViewportOptions.MainViewport(), new ClaimsPrincipal());

            var parameters = new TestParameters(canDeactivate: true, isAuthorized: true,
                                                newViewModel: _viewModelInit1ParamsReturnsTask,
                                                routeRequest: routeRequest);
            var routeResult = await RunTest(parameters);
            routeResult.Success.ShouldBe(true);
            routeResult.StatusCode.ShouldBe(RouteResultStatusCode.OK);

            _fixture.GetMock<IViewModelInit1ParamsReturnsTask>()
                .Verify();
        }

        [WpfFact()]
        [Trait("Category", "Instant")]
        public async Task ViewAwareVmReceivesView()
        {
            _fixture.GetMock<IViewAwareVm>()
                .Setup(z => z.LoadView(It.IsAny<object>()))
                .Verifiable();

            var routeRequest = new RouteRequest("", (decimal)25, ViewportOptions.MainViewport(), new ClaimsPrincipal());

            var parameters = new TestParameters(canDeactivate: true, isAuthorized: true,
                                                newViewModel: _viewAwareViewModel,
                                                routeRequest: routeRequest);
            var routeResult = await RunTest(parameters);
            routeResult.Success.ShouldBe(true);
            routeResult.StatusCode.ShouldBe(RouteResultStatusCode.OK);

            _fixture.GetMock<IViewAwareVm>()
                .Verify();
        }



        public class TestParameters
        {
            public bool CanDeactivate { get; set; }
            public bool IsAuthorized { get; set; }
            public object NewViewModel { get; }
            public RouteRequest RouteRequest { get; set; }

            public TestParameters(bool canDeactivate, bool isAuthorized, object newViewModel, RouteRequest routeRequest = null)
            {
                CanDeactivate = canDeactivate;
                IsAuthorized = isAuthorized;
                NewViewModel = newViewModel;
                RouteRequest = routeRequest ?? new RouteRequest("uri", null, ViewportOptions.MainViewport(), new ClaimsPrincipal());
            }
        }
    }

    public static class ObjectExtensions
    {
        public static Task<T> ToTask<T>(this T obj)
        {
            return Task.FromResult(obj);
        }
    }
}
