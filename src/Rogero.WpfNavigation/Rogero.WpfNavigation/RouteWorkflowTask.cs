using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Rogero.Options;
using Serilog;

namespace Rogero.WpfNavigation
{
    public class RouteWorkflowTask
    {
        public static async Task<RouteResult> Go(
            RouteRequest routeRequest,
            IRouteEntryRegistry routeEntryRegistry,
            IRouteAuthorizationManager routeAuthorizationManager,
            IRouterService routerService,
            ILogger logger)
        {
            var workflow = new RouteWorkflowTask(routeRequest, routeEntryRegistry, routeAuthorizationManager, routerService, logger);
            return await workflow.GoAsync();
        }

        public string Uri => _routeRequest.Uri;
        public object InitData => _routeRequest.InitData;
        public string ViewportName => _routeRequest.TargetViewportName;
        public Guid RoutingWorkflowId { get; } = Guid.NewGuid();

        private readonly ILogger _logger;
        private readonly IRouteEntryRegistry _routeEntryRegistry;
        private readonly IRouteAuthorizationManager _routeAuthorizationManager;
        private readonly IRouterService _routerService;

        private readonly RouteRequest _routeRequest;
        private Option<IRouteEntry> _routeEntry;

        internal RouteWorkflowTask(
            RouteRequest routeRequest,
            IRouteEntryRegistry routeEntryRegistry,
            IRouteAuthorizationManager routeAuthorizationManager,
            IRouterService routerService,
            ILogger logger)
        {
            _routeRequest = routeRequest;

            _routeEntryRegistry = routeEntryRegistry;
            _routeAuthorizationManager = routeAuthorizationManager;
            _routerService = routerService;
            _logger = logger
                .ForContext("Class", "RouteWorkflowTask")
                .ForContext("Uri", Uri)
                .ForContext("ViewportName", ViewportName)
                .ForContext("InitData", InitData)
                .ForContext("RouterServiceId", _routerService.RouterServiceId)
                .ForContext("RoutingWorkflowId", RoutingWorkflowId);
        }

        internal async Task<RouteResult> GoAsync()
        {
            var initDataIsNull = InitData == null ? "with init data" : "without init data";
            using (Logging.Timing(_logger, $"navigation workflow URI: [{Uri}], in viewport [{ViewportName}] " + initDataIsNull))
            {
                try
                {
                    _routeEntry = GetRouteEntry();
                    if (_routeEntry.HasNoValue) return new RouteResult(RouteResultStatusCode.RouteNotFound);

                    var routeContext = new RoutingContext(_routeEntry.Value, _routeRequest);
                    var authorized = await CheckRouteAuthorizationAsync(routeContext);
                    if (!authorized) return new RouteResult(RouteResultStatusCode.Unauthorized);

                    var canDeactivate = await CanDeactivateCurrentRouteAsync(_routeRequest.TargetViewportName);
                    if (!canDeactivate) return new RouteResult(RouteResultStatusCode.CanDeactiveFailed);

                    var canActivate = await CanActivateNewRouteAsync();
                    if (!canActivate) return new RouteResult(RouteResultStatusCode.CanActivateFailed);

                    var viewModel = GetViewModel(_routeEntry.Value);
                    await InitializeViewModelAsync(viewModel);
                    
                    var view = GetView(_routeEntry.Value);
                    AssignDataContext(view, viewModel);

                    if (viewModel is IViewAware viewAware) AssignViewToViewModel(view, viewAware);

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

        private void AssignViewToViewModel(UIElement view, IViewAware viewAware)
        {
            LogInfo("ViewModel {ViewModelType} is IViewAware so calleding LoadView() with the View {ViewType}", 
                    viewAware.GetType().FullName, 
                    view.GetType().FullName);
            viewAware.LoadView(view);
        }

        private async Task<bool> CheckRouteAuthorizationAsync(RoutingContext routingContext)
        {
            try
            {

                var routeAuthResult = await _routeAuthorizationManager.CheckAuthorization(routingContext);
                var granted = routeAuthResult.Equals(RouteAuthorizationResult.Granted);

                if (granted) LogInfo("RouteEntry authorization granted.");
                else LogInfo("RouteEntry authorization denied.");

                return granted;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Exception checking route authorization.");
                throw;
            }
        }

        private Option<IRouteEntry> GetRouteEntry()
        {
            LogInfo($"Finding RouteEntry for uri: {Uri}");
            var routeEntry = _routeEntryRegistry.GetRouteEntry(Uri);
            if (routeEntry.HasNoValue)
                LogInfo("Did not find RouteEntry");
            else
                LogInfo("Found RouteEntry, ViewType: {ViewType}", routeEntry.Value.ViewType);
            return routeEntry;
        }

        private async Task<bool> CanDeactivateCurrentRouteAsync(string viewportName)
        {
            var currentViewModel = _routerService.GetActiveDataContext(viewportName);
            if (currentViewModel.HasNoValue)
            {
                LogInfo("No current viewmodel to call CanDeactivate upon");
                return true;
            }
            if (currentViewModel.Value is ICanDeactivate canDeactivate)
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
            return true;
        }

        private object GetViewModel(IRouteEntry routeEntry)
        {
            LogInfo("Creating viewmodel of type {ViewModelType}", routeEntry.ViewModelType);
            var viewModel = routeEntry.CreateViewModel();
            LogInfo("Created viewmodel of type: {ViewModelType}", routeEntry.ViewModelType);
            return viewModel;
        }

        private async Task InitializeViewModelAsync(object viewModel)
        {
            var initMethod = GetViewModelInitMethod(viewModel);
            if (initMethod == null)
            {
                LogInfo("Viewmodel has no Init method");
                return;
            }
            var parameterCount = initMethod.GetParameters().Length;
            if (parameterCount == 0)
            {
                LogInfo("Initializing viewmodel with no parameters");
                if (InitData != null) LogWarning("Viewmodel Init method has no parameters, but InitData was passed to this route request.");
                var result = initMethod.Invoke(viewModel, new object[] { });
                LogInfo("Viewmodel initialization called");
                await AwaitResultIfNecessary(result);
                LogInfo("Viewmodel initialization returned");
            }
            else if (parameterCount == 1)
            {
                LogInfo("Initializing viewmodel with InitData of type {InitDataType}", InitData?.GetType());
                if (InitData == null)
                    LogWarning("Passing null to a new viewmodel {ViewModelType} that has a paramter in the Init method", viewModel.GetType());

                var result = initMethod.Invoke(viewModel, new[] { InitData });
                LogInfo("Viewmodel initialized with InitData");
                await AwaitResultIfNecessary(result);
                LogInfo("Viewmodel initialization with InitData returned");
            }
            else if (parameterCount > 1)
            {
                var exception = new NotImplementedException();
                LogError("ViewModel init method has more than 1 parameter and this is not supported at this time.", exception);
                throw exception;
            }
        }

        private static async Task AwaitResultIfNecessary(object result)
        {
            if (result is Task task)
            {
                await task;
            }
        }

        private static MethodInfo GetViewModelInitMethod(object viewModel)
        {
            var initMethods = viewModel.GetType()
                .GetMethods()
                .Where(z => z.Name.StartsWith("Init"))
                .ToList();

            return initMethods.FirstOrDefault();
        }

        private UIElement GetView(IRouteEntry routeEntry)
        {
            LogInfo("Creating the view.");
            var uiElement = routeEntry.CreateView();
            LogInfo("View, {ViewType}, created.", uiElement.GetType());
            return uiElement;
        }

        private void AssignDataContext(object view, object viewModel)
        {
            if (view is FrameworkElement frameworkElement)
            {
                LogInfo("Assigning viewmodel ({ViewModelType}) to view ({ViewType}) DataContext", viewModel.GetType(), frameworkElement.GetType());
                frameworkElement.DataContext = viewModel;
                LogInfo("Assigned viewmodel to view DataContext.");
            }
            else
            {
                if (viewModel != null)
                {
                    LogWarning(
                        "Viewmodel of type {ViewModelType} was created but was not assigned to the View of type {ViewType} since the view does not have a DataContext property",
                        viewModel.GetType(), view.GetType());
                }
                else
                {
                    LogInfo(
                        "The viewmodel was null which is good because the view {ViewType} does not derive from FrameworkElement and does not have a DataContext property to assign to.",
                        view.GetType());
                }
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
                return new RouteResult(RouteResultStatusCode.NoViewportFound);
            }
        }

        private void LogInfo(string message) => _logger.Information(message);
        private void LogInfo<T>(string message, T data) => _logger.Information(message, data);
        private void LogInfo<T, T1>(string message, T data, T1 data1) => _logger.Information(message, data, data1);
        private void LogInfo<T, T1, T2>(string message, T data, T1 data1, T2 data2) => _logger.Information(message, data, data1, data2);

        private void LogWarning(string message) => _logger.Warning(message);
        private void LogWarning<T>(string message, T data) => _logger.Warning(message, data);
        private void LogWarning<T, T1>(string message, T data, T1 data1) => _logger.Warning(message, data, data1);

        private void LogError<T>(string message, T data) => _logger.Error((Exception)null, message, data);
    }
}