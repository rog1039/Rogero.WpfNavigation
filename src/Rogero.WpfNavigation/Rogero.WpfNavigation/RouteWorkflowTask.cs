using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Rogero.Options;
using Serilog;

namespace Rogero.WpfNavigation
{
    public class RouteWorkflowTask
    {
        public static async Task<RouteResult> Go(string uri, object initData, string viewportName, RouterService routerService)
        {
            var workflow = new RouteWorkflowTask(uri, initData, viewportName, routerService);
            return await workflow.Go();
        }

        public string Uri { get; }
        public object InitData { get; }
        public string ViewportName { get; }
        public Guid RoutingWorkflowId { get; } = Guid.NewGuid();

        private readonly ILogger _logger;
        private readonly RouterService _routerService;

        private RouteWorkflowTask(string uri, object initData, string viewportName, RouterService routerService)
        {
            Uri = uri;
            InitData = initData;
            ViewportName = viewportName;
            _routerService = routerService;
            _logger = _routerService._logger
                .ForContext("Class", "RouteWorkflowTask")
                .ForContext("Uri", uri)
                .ForContext("ViewportName", viewportName)
                .ForContext("InitData", initData)
                .ForContext("RouterServiceId", _routerService.RouterServiceId)
                .ForContext("RoutingWorkflowId", RoutingWorkflowId);
        }

        private async Task<RouteResult> Go()
        {
            var initDataIsNull = InitData == null ? "with init data" : "without init data";
            using (Logging.Timing(_logger, $"navigation workflow URI: [{Uri}], in viewport [{ViewportName}] " + initDataIsNull))
            {
                try
                {
                    var viewVmPair = GetViewVmPair();
                    if (viewVmPair.HasNoValue) return new RouteResult(RouteResultStatusCode.RouteNotFound);

                    var canDeactivate = await CanDeactivateCurrentRoute();
                    if (!canDeactivate) return new RouteResult(RouteResultStatusCode.CanDeactiveFailed);

                    var canActivate = await CanActivateNewRoute();
                    if (!canActivate) return new RouteResult(RouteResultStatusCode.CanActivateFailed);

                    var viewModel = GetViewModel(viewVmPair.Value);
                    await InitializeViewModel(viewModel);
                    var view = GetView(viewVmPair.Value);
                    AssignDataContext(view, viewModel);

                    var routeResult = AddViewToUi(view);
                    return routeResult;
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Exception during RouteWorkflowTask.Go() method.");
                    throw;
                }
            }
        }

        private Option<ViewVmPair> GetViewVmPair()
        {
            LogInfo("Finding ViewVm Pair.");
            var pair = _routerService.GetViewVmPair(Uri, InitData);
            if (pair.HasNoValue)
                LogInfo("Did not find ViewVmPair");
            else
                LogInfo("Found ViewVm Pair, ViewType: {ViewType}", pair.Value.ViewType);
            return pair;
        }

        private async Task<bool> CanDeactivateCurrentRoute()
        {
            var currentViewModel = CurrentViewModel;
            if (currentViewModel == null)
            {
                LogInfo("No current viewmodel to call CanDeactivate upon");
                return true;
            }
            if (currentViewModel is ICanDeactivate canDeactivate)
            {
                LogInfo("Current viewmodel, {CurrentViewModelType} implements CanDeactivate", canDeactivate.GetType().FullName);
                var canDeactivateResponse = await canDeactivate.CanDeactivate(Uri, InitData);
                LogInfo("CanDeactivate returned {CanDeactivateResponse}", canDeactivateResponse);
                return canDeactivateResponse;
            }
            LogInfo("Current viewmodel, {CurrentViewModelType}, does not implement CanDeactivate", currentViewModel.GetType().FullName);
            return true;
        }

        private Task<bool> CanActivateNewRoute()
        {
            LogInfo("CanActivate always returns true currently.");
            return Task.FromResult(true);
        }

        private object GetViewModel(ViewVmPair value)
        {
            LogInfo("Creating viewmodel.");
            var viewModel = value.CreateViewModel();
            LogInfo("Created viewmodel of type: {ViewModelType}", viewModel.GetType());
            return viewModel;
        }

        private async Task InitializeViewModel(object viewModel)
        {
            var initMethod = viewModel.GetType().GetMethod("Init");
            if (initMethod == null)
            {
                LogInfo("Viewmodel has no Init method");
                return;
            }
            var parameterCount = initMethod.GetParameters().Length;
            if (parameterCount == 0)
            {
                LogInfo("Initializing viewmodel with no parameters");
                var result = initMethod.Invoke(viewModel, new object[] {});
                LogInfo("Viewmodel initialization called");
                if (result.GetType().IsSubclassOf(typeof(Task)))
                {
                    var task = (Task) result;
                    await task;
                }
                LogInfo("Viewmodel initialization returned");
            }
            else if (parameterCount == 1)
            {
                LogInfo("Initializing viewmodel with InitData");
                var result = initMethod.Invoke(viewModel, new[] { InitData });
                LogInfo("Viewmodel initialized with InitData");
                if (result.GetType().IsSubclassOf(typeof(Task)))
                {
                    var task = (Task)result;
                    await task;
                }
                LogInfo("Viewmodel initialization with InitData returned");
            }
            else if (parameterCount > 1)
            {
                throw new NotImplementedException();
            }
        }

        private UIElement GetView(ViewVmPair value)
        {
            LogInfo("Creating the view.");
            var uiElement = value.CreateView();
            LogInfo("View, {ViewType}, created.", uiElement.GetType());
            return uiElement;
        }

        private void AssignDataContext(object view, object viewModel)
        {
            if (view is FrameworkElement)
            {
                LogInfo("Assigning viewmodel ({ViewModelType}) to view ({ViewType}) DataContext", viewModel.GetType(), view.GetType());
                var fe = (FrameworkElement)view;
                fe.DataContext = viewModel;
                LogInfo("Assigned viewmodel to view DataContext.");
            }
            else
            {
                LogInfo("Did not assign viewmodel to datacontext since the view {ViewType} does not derive from FrameworkElement", view.GetType());
            }
        }

        private RouteResult AddViewToUi(UIElement view)
        {
            var viewport = _routerService.GetControlViewportAdapter(ViewportName);
            if (viewport.HasValue)
            {
                LogInfo("Found target viewport, {ViewportName}, of type {ViewportType}. Adding view to viewport.", ViewportName, viewport.Value.GetType());
                viewport.Value.AddControl(view);
                LogInfo("View {ViewType} added to viewport {ViewportName}, type: {ViewportType}", view.GetType(), ViewportName, viewport.Value.GetType());
                return RouteResult.Succeeded;
            }
            else
            {
                LogError("No viewport found with specified viewport name, {ViewportName}", ViewportName);
                //TODO: Report no viewport found somehow!
                return new RouteResult(RouteResultStatusCode.NoViewportFound);
            }
        }
        
        private object CurrentViewModel => new NotImplementedException();
        
        private void LogInfo(string message) => _logger.Information(message);
        private void LogInfo<T>(string message, T data) => _logger.Information(message, data);
        private void LogInfo<T,T1>(string message, T data, T1 data1) => _logger.Information(message, data, data1);
        private void LogInfo<T, T1,T2>(string message, T data, T1 data1, T2 data2) => _logger.Information(message, data, data1, data2);

        private void LogError<T>(string message, T data) => _logger.Error((Exception)null, message, data);
    }

    public class RouteWorkflowTask2
    {
        public static async Task<RouteResult> Go(string uri, object initData, string viewportName, IRouteEntryRegistry routeEntryRegistry, IRouteAuthorizationManager routeAuthorizationManager, RouterService routerService)
        {
            var workflow = new RouteWorkflowTask2(uri, initData, viewportName, routeEntryRegistry, routeAuthorizationManager, routerService);
            return await workflow.GoAsync();
        }

        public string Uri { get; }
        public object InitData { get; }
        public string ViewportName { get; }
        public Guid RoutingWorkflowId { get; } = Guid.NewGuid();

        private readonly ILogger _logger;
        private readonly IRouteEntryRegistry _routeEntryRegistry;
        private readonly IRouteAuthorizationManager _routeAuthorizationManager;
        private readonly RouterService _routerService;

        private RouteWorkflowTask2(string uri, object initData, string viewportName, IRouteEntryRegistry routeEntryRegistry, IRouteAuthorizationManager routeAuthorizationManager, RouterService routerService)
        {
            Uri = uri;
            InitData = initData;
            ViewportName = viewportName;

            _routeEntryRegistry = routeEntryRegistry;
            _routeAuthorizationManager = routeAuthorizationManager;
            _routerService = routerService;

            _logger = _routerService._logger
                .ForContext("Class", "RouteWorkflowTask2")
                .ForContext("Uri", uri)
                .ForContext("ViewportName", viewportName)
                .ForContext("InitData", initData)
                .ForContext("RouterServiceId", _routerService.RouterServiceId)
                .ForContext("RoutingWorkflowId", RoutingWorkflowId);
        }
        

        private async Task<RouteResult> GoAsync()
        {
            var initDataIsNull = InitData == null ? "with init data" : "without init data";
            using (Logging.Timing(_logger, $"navigation workflow URI: [{Uri}], in viewport [{ViewportName}] " + initDataIsNull))
            {
                try
                {
                    var routeEntry = GetRouteEntry();
                    if (routeEntry.HasNoValue) return new RouteResult(RouteResultStatusCode.RouteNotFound);

                    var authorized = await CheckRouteAuthorizationAsync(routeEntry.Value);
                    if (!authorized) return new RouteResult(RouteResultStatusCode.Unauthorized);

                    var canDeactivate = await CanDeactivateCurrentRouteAsync();
                    if (!canDeactivate) return new RouteResult(RouteResultStatusCode.CanDeactiveFailed);

                    var canActivate = await CanActivateNewRouteAsync();
                    if (!canActivate) return new RouteResult(RouteResultStatusCode.CanActivateFailed);

                    var viewModel = GetViewModel(routeEntry.Value);
                    await InitializeViewModelAsync(viewModel);
                    var view = GetView(routeEntry.Value);
                    AssignDataContext(view, viewModel);

                    var routeResult = AddViewToUi(view);
                    return routeResult;
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Exception during RouteWorkflowTask2.Go() method.");
                    throw;
                }
            }
        }

        private async Task<bool> CheckRouteAuthorizationAsync(IRouteEntry routeEntryValue)
        {
            return await Task.FromResult(false);
        }

        private Option<IRouteEntry> GetRouteEntry()
        {
            LogInfo("Finding RouteEntry.");
            var routeEntry = _routeEntryRegistry.GetRouteEntry(Uri);
            if (routeEntry.HasNoValue)
                LogInfo("Did not find RouteEntry");
            else
                LogInfo("Found RouteEntry, ViewType: {ViewType}", routeEntry.Value.View);
            return routeEntry;
        }

        private async Task<bool> CanDeactivateCurrentRouteAsync()
        {
            var currentViewModel = CurrentViewModel;
            if (currentViewModel == null)
            {
                LogInfo("No current viewmodel to call CanDeactivate upon");
                return true;
            }
            if (currentViewModel is ICanDeactivate canDeactivate)
            {
                LogInfo("Current viewmodel, {CurrentViewModelType} implements CanDeactivate", canDeactivate.GetType().FullName);
                var canDeactivateResponse = await canDeactivate.CanDeactivate(Uri, InitData);
                LogInfo("CanDeactivate returned {CanDeactivateResponse}", canDeactivateResponse);
                return canDeactivateResponse;
            }
            LogInfo("Current viewmodel, {CurrentViewModelType}, does not implement CanDeactivate", currentViewModel.GetType().FullName);
            return true;
        }

        private async Task<bool> CanActivateNewRouteAsync()
        {
            LogInfo("CanActivate always returns true currently.");
            return await Task.FromResult(true);
        }

        private object GetViewModel(IRouteEntry routeEntry)
        {
            LogInfo("Creating viewmodel of type {ViewModelType}", routeEntry.Controller);
            var viewModel = Activator.CreateInstance(routeEntry.Controller);
            LogInfo("Created viewmodel of type: {ViewModelType}", routeEntry.Controller);
            return viewModel;
        }

        private async Task InitializeViewModelAsync(object viewModel)
        {
            var initMethod = viewModel.GetType().GetMethod("Init");
            if (initMethod == null)
            {
                LogInfo("Viewmodel has no Init method");
                return;
            }
            var parameterCount = initMethod.GetParameters().Length;
            if (parameterCount == 0)
            {
                LogInfo("Initializing viewmodel with no parameters");
                var result = initMethod.Invoke(viewModel, new object[] { });
                LogInfo("Viewmodel initialization called");
                if (result.GetType().IsSubclassOf(typeof(Task)))
                {
                    var task = (Task)result;
                    await task;
                }
                LogInfo("Viewmodel initialization returned");
            }
            else if (parameterCount == 1)
            {
                LogInfo("Initializing viewmodel with InitData");
                var result = initMethod.Invoke(viewModel, new[] { InitData });
                LogInfo("Viewmodel initialized with InitData");
                if (result.GetType().IsSubclassOf(typeof(Task)))
                {
                    var task = (Task)result;
                    await task;
                }
                LogInfo("Viewmodel initialization with InitData returned");
            }
            else if (parameterCount > 1)
            {
                throw new NotImplementedException();
            }
        }

        private UIElement GetView(IRouteEntry routeEntry)
        {
            LogInfo("Creating the view.");
            var uiElement = (UIElement) Activator.CreateInstance(routeEntry.View);
            LogInfo("View, {ViewType}, created.", uiElement.GetType());
            return uiElement;
        }

        private void AssignDataContext(object view, object viewModel)
        {
            if (view is FrameworkElement)
            {
                LogInfo("Assigning viewmodel ({ViewModelType}) to view ({ViewType}) DataContext", viewModel.GetType(), view.GetType());
                var fe = (FrameworkElement)view;
                fe.DataContext = viewModel;
                LogInfo("Assigned viewmodel to view DataContext.");
            }
            else
            {
                LogInfo("Did not assign viewmodel to datacontext since the view {ViewType} does not derive from FrameworkElement", view.GetType());
            }
        }

        private RouteResult AddViewToUi(UIElement view)
        {
            var viewport = _routerService.GetControlViewportAdapter(ViewportName);
            if (viewport.HasValue)
            {
                LogInfo("Found target viewport, {ViewportName}, of type {ViewportType}. Adding view to viewport.", ViewportName, viewport.Value.GetType());
                viewport.Value.AddControl(view);
                LogInfo("View {ViewType} added to viewport {ViewportName}, type: {ViewportType}", view.GetType(), ViewportName, viewport.Value.GetType());
                return RouteResult.Succeeded;
            }
            else
            {
                LogError("No viewport found with specified viewport name, {ViewportName}", ViewportName);
                //TODO: Report no viewport found somehow!
                return new RouteResult(RouteResultStatusCode.NoViewportFound);
            }
        }

        private object CurrentViewModel => new NotImplementedException();

        private void LogInfo(string message) => _logger.Information(message);
        private void LogInfo<T>(string message, T data) => _logger.Information(message, data);
        private void LogInfo<T, T1>(string message, T data, T1 data1) => _logger.Information(message, data, data1);
        private void LogInfo<T, T1, T2>(string message, T data, T1 data1, T2 data2) => _logger.Information(message, data, data1, data2);

        private void LogError<T>(string message, T data) => _logger.Error((Exception)null, message, data);
    }
}