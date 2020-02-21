using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using Rogero.Options;
using Serilog;

namespace Rogero.WpfNavigation
{
    public static class RouteWorkflow
    {
        public static Option<IRouteEntry> GetRouteEntry(ILogger logger, IRouteEntryRegistry routeEntryRegistry, string uri)
        {
            logger.Information($"Finding RouteEntry for uri: {uri}");
            var routeEntry = routeEntryRegistry.GetRouteEntry(uri);
            if (routeEntry.HasNoValue)
                logger.Warning("Did not find RouteEntry");
            else
                logger.Information("Found RouteEntry, ViewType: {ViewType}", routeEntry.Value.ViewType);
            return routeEntry;
        }
        
        public static void AssignViewToViewModel(ILogger logger, UIElement view, IViewAware viewAware)
        {
            logger.Information("ViewModel {ViewModelType} is IViewAware so calling LoadView() with the View {ViewType}", 
                viewAware.GetType().FullName, 
                view.GetType().FullName);
            viewAware.LoadView(view);
        }
        
        public static object GetViewModel(ILogger logger, IRouteEntry routeEntry)
        {
            logger.Information("Creating viewmodel of type {ViewModelType}", routeEntry.ViewModelType);
            var viewModel = routeEntry.CreateViewModel();
            logger.Information("Created viewmodel of type: {ViewModelType}", routeEntry.ViewModelType);
            return viewModel;
        }

        public static UIElement GetView(ILogger logger, IRouteEntry routeEntry)
        {
            logger.Information("Creating the view.");
            var uiElement = routeEntry.CreateView();
            logger.Information("View, {ViewType}, created.", uiElement.GetType());
            return uiElement;
        }
        
        public static async Task<bool> CheckRouteAuthorizationAsync(
            ILogger logger,
            IRouteAuthorizationManager routeAuthorizationManager, 
            RoutingContext routingContext)
        {
            try
            {

                var routeAuthResult = await routeAuthorizationManager.CheckAuthorization(routingContext);
                var granted = routeAuthResult.Equals(RouteAuthorizationResult.Granted);

                logger.Information(granted
                    ? "RouteEntry authorization granted."
                    : "RouteEntry authorization denied.");

                return granted;
            }
            catch (Exception e)
            {
                logger.Error(e, "Exception checking route authorization.");
                throw;
            }
        }
        
        public static async Task InitViewModel(
            ILogger logger,
            object initData,
            object viewModel
            )
        {
            var initMethod = GetViewModelInitMethod(viewModel);
            if (initMethod == null)
            {
                logger.Information("Viewmodel has no Init method");
                return;
            }
            var parameterCount = initMethod.GetParameters().Length;
            if (parameterCount == 0)
            {
                logger.Information("Initializing viewmodel with no parameters");
                if (initData != null) logger.Warning("Viewmodel Init method has no parameters, but InitData was passed to this route request.");
                var result = initMethod.Invoke(viewModel, new object[] { });
                logger.Information("Viewmodel initialization called");
                await result.AwaitIfNecessary();
                logger.Information("Viewmodel initialization returned");
            }
            else if (parameterCount == 1)
            {
                logger.Information("Initializing viewmodel with InitData of type {InitDataType}", initData?.GetType());
                if (initData == null)
                    logger.Warning("Passing null to a new viewmodel {ViewModelType} that has a paramter in the Init method", viewModel.GetType());

                var result = initMethod.Invoke(viewModel, new[] { initData });
                logger.Information("Viewmodel initialized with InitData");
                await result.AwaitIfNecessary();
                logger.Information("Viewmodel initialization with InitData returned");
            }
            else if (parameterCount > 1)
            {
                var exception = new NotImplementedException();
                logger.Error("ViewModel init method has more than 1 parameter and this is not supported at this time.", exception);
                throw exception;
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
        

        public static void AssignDataContext(ILogger logger, object view, object viewModel)
        {
            if (view is FrameworkElement frameworkElement)
            {
                logger.Information("Assigning viewmodel ({ViewModelType}) to view ({ViewType}) DataContext", viewModel.GetType(), frameworkElement.GetType());
                frameworkElement.DataContext = viewModel;
                logger.Information("Assigned viewmodel to view DataContext.");
            }
            else
            {
                if (viewModel != null)
                {
                    logger.Warning(
                        "Viewmodel of type {ViewModelType} was created but was not assigned to the View of type {ViewType} since the view does not have a DataContext property",
                        viewModel.GetType(), view.GetType());
                }
                else
                {
                    logger.Information(
                        "The viewmodel was null which is good because the view {ViewType} does not derive from FrameworkElement and does not have a DataContext property to assign to.",
                        view.GetType());
                }
            }
        }
        

        public static async Task<bool> CanDeactivateCurrentRouteAsync(ILogger logger, IRouterService routerService, string viewportName, string uri, object initData)
        {
            var currentViewModel = routerService.GetActiveDataContext(viewportName);
            if (currentViewModel.HasNoValue)
            {
                logger.Information("No current viewmodel to call CanDeactivate upon");
                return true;
            }
            if (currentViewModel.Value is ICanDeactivate canDeactivate)
            {
                logger.Information("Current viewmodel, {CurrentViewModelType} implements CanDeactivate", canDeactivate.GetType().FullName);
                var canDeactivateResponse = await canDeactivate.CanDeactivate(uri, initData);
                logger.Information("CanDeactivate returned {CanDeactivateResponse}", canDeactivateResponse);
                return canDeactivateResponse;
            }
            logger.Information("Current viewmodel, {CurrentViewModelType}, does not implement CanDeactivate", currentViewModel.GetType().FullName);
            return true;
        }

        public static async Task<bool> CanActivateNewRouteAsync(ILogger logger)
        {
            logger.Information("CanActivate always returns true currently.");
            return true;
        }

        public static RouteResult AddViewToUi(ILogger logger, RouteWorkflowTask routeWorkflowTask, IRouterService _routerService, string viewportName, UIElement view)
        {
            var viewport = _routerService.GetControlViewportAdapter(viewportName);
            if (viewport.HasValue)
            {
                logger.Information("Found target viewport, {ViewportName}, of type {ViewportType}. Adding view to viewport.", viewportName, viewport.Value.GetType());
                viewport.Value.AddControl(view, routeWorkflowTask);
                logger.Information("View {ViewType} added to viewport {ViewportName}, type: {ViewportType}", view.GetType(), viewportName, viewport.Value.GetType());
                return RouteResult.Succeeded;
            }
            else
            {
                logger.Error("No viewport found with specified viewport name, {ViewportName}", viewportName);
                return new RouteResult(RouteResultStatusCode.NoViewportFound);
            }
        }
    }
    
    public class ViewControllerPair
    {
        public FrameworkElement View { get; set; }
        public object Controller { get; set; }

        public ViewControllerPair(FrameworkElement view, object controller)
        {
            View = view;
            Controller = controller;
        }
    }
    
    public class RouteWorkflowTask
    {
        public static RouteWorkflowTask Go(
            RouteRequest routeRequest,
            IRouteEntryRegistry routeEntryRegistry,
            IRouteAuthorizationManager routeAuthorizationManager,
            IRouterService routerService,
            ILogger logger)
        {
            var workflow = new RouteWorkflowTask(routeRequest, routeEntryRegistry, routeAuthorizationManager, routerService, logger);
            workflow.RouteResult = workflow.GoAsync();
            return workflow;
        }

        public string Uri => _routeRequest.Uri;
        public object InitData => _routeRequest.InitData;
        public string ViewportName => _routeRequest.TargetViewportName;
        public Option<string> RouteName => RouteEntry.Value?.Name.ToOption();
        public Guid RoutingWorkflowId { get; } = Guid.NewGuid();
        public Option<IRouteEntry> RouteEntry { get; private set; }
        public Guid RouteRequestId => _routeRequest.RouteRequestId;
        public Task<RouteResult> RouteResult { get; private set; }
        public DateTime Started { get; } = DateTime.UtcNow;
        
        public object Controller { get; private set; }
        public UIElement View { get; private set; }

        private readonly ILogger _logger;
        private readonly IRouteEntryRegistry _routeEntryRegistry;
        private readonly IRouteAuthorizationManager _routeAuthorizationManager;
        private readonly IRouterService _routerService;

        private readonly RouteRequest _routeRequest;

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
            var initDataIsNull = InitData != null ? "with init data" : "without init data";
            using (Logging.Timing(_logger, $"navigation workflow URI: [{Uri}], in viewport [{ViewportName}] " + initDataIsNull))
            {
                try
                {
                    RouteEntry = RouteWorkflow.GetRouteEntry(_logger, _routeEntryRegistry, _routeRequest.Uri);
                    if (RouteEntry.HasNoValue) return new RouteResult(RouteResultStatusCode.RouteNotFound);

                    //Check authorization
                    var routeContext = new RoutingContext(RouteEntry.Value, _routeRequest);
                    var authorized = await RouteWorkflow.CheckRouteAuthorizationAsync(_logger, _routeAuthorizationManager,routeContext);
                    if (!authorized) return new RouteResult(RouteResultStatusCode.Unauthorized);

                    //Can deactivate current
                    var canDeactivate = await RouteWorkflow.CanDeactivateCurrentRouteAsync(_logger, _routerService, _routeRequest.TargetViewportName, Uri, InitData);
                    if (!canDeactivate) return new RouteResult(RouteResultStatusCode.CanDeactiveFailed);

                    //Activate new route
                    var canActivate = await RouteWorkflow.CanActivateNewRouteAsync(_logger);
                    if (!canActivate) return new RouteResult(RouteResultStatusCode.CanActivateFailed);

                    //Get ViewModel
                    Controller = RouteWorkflow.GetViewModel(_logger, RouteEntry.Value);
                    await RouteWorkflow.InitViewModel(_logger, _routeRequest.InitData, Controller);
                    //Get View
                    View = RouteWorkflow.GetView(_logger, RouteEntry.Value);
                    RouteWorkflow.AssignDataContext(_logger, View, Controller);

                    //IViewAware context
                    if (Controller is IViewAware viewAware) RouteWorkflow.AssignViewToViewModel(_logger, View, viewAware);
                    
                    //Add View to UI
                    var routeResult = RouteWorkflow.AddViewToUi(_logger, this, _routerService, ViewportName, View);
                    
                    return routeResult;
                }
                catch (Exception e)
                {
                    _logger.Error(e, "Exception during RouteWorkflowTask.Go() method.");
                    throw;
                }
            }
        }

        public override string ToString()
        {
            return $"{RouteName}: {Uri} -> {ViewportName}";
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