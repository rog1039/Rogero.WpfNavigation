using System;
using System.Threading.Tasks;
using System.Windows;
using Rogero.Options;
using Serilog;

namespace Rogero.WpfNavigation
{
    public class RouteWorkflowTask
    {
        public static RouteWorkflowTask Go(
            RouteRequest               routeRequest,
            IRouteEntryRegistry        routeEntryRegistry,
            IRouteAuthorizationManager routeAuthorizationManager,
            IRouterService             routerService,
            ILogger                    logger)
        {
            var workflow = new RouteWorkflowTask(routeRequest, routeEntryRegistry, routeAuthorizationManager, routerService,
                                                 logger);
            workflow.RouteResult = workflow.GoAsync();
            return workflow;
        }

        public string          Uri             => _routeRequest.Uri;
        public object          InitData        => _routeRequest.InitData;
        public ViewportOptions ViewportOptions => _routeRequest.ViewportOptions;
        public Guid            RouteRequestId  => _routeRequest.RouteRequestId;
        public Option<string>  RouteName       => RouteEntry.Value?.Name.ToOption();

        public Guid                RoutingWorkflowId { get; } = Guid.NewGuid();
        public DateTime            StartedTime       { get; } = DateTime.UtcNow;
        public DateTime            FinishedTime      { get; private set; }
        public Option<IRouteEntry> RouteEntry        { get; private set; }
        public Task<RouteResult>   RouteResult       { get; private set; }

        public object    Controller { get; private set; }
        public UIElement View       { get; private set; }

        private readonly ILogger                    _logger;
        private readonly IRouteEntryRegistry        _routeEntryRegistry;
        private readonly IRouteAuthorizationManager _routeAuthorizationManager;
        private readonly IRouterService             _routerService;

        private readonly RouteRequest _routeRequest;

        internal RouteWorkflowTask(
            RouteRequest               routeRequest,
            IRouteEntryRegistry        routeEntryRegistry,
            IRouteAuthorizationManager routeAuthorizationManager,
            IRouterService             routerService,
            ILogger                    logger)
        {
            _routeRequest = routeRequest;

            _routeEntryRegistry        = routeEntryRegistry;
            _routeAuthorizationManager = routeAuthorizationManager;
            _routerService             = routerService;
            _logger = logger
                .ForContext("Class",             "RouteWorkflowTask")
                .ForContext("Uri",               Uri)
                .ForContext("ViewportName",      ViewportOptions.ToString())
                .ForContext("InitData",          InitData)
                .ForContext("RouterServiceId",   _routerService.RouterServiceId)
                .ForContext("RoutingWorkflowId", RoutingWorkflowId);
        }

        internal async Task<RouteResult> GoAsync()
        {
            var initDataIsNull = InitData != null ? "with init data" : "without init data";
            using (Logging.Timing(_logger, $"navigation workflow URI: [{Uri}], in viewport [{ViewportOptions}] " + initDataIsNull)
            )
            {
                try
                {
                    RouteEntry = RouteWorkflow.GetRouteEntry(_logger, _routeEntryRegistry, Uri);
                    if (RouteEntry.HasNoValue) return new RouteResult(RouteResultStatusCode.RouteNotFound);

                    //Check authorization
                    var routeContext = new RoutingContext(RouteEntry.Value, _routeRequest);
                    var authorized =
                        await RouteWorkflow.CheckRouteAuthorizationAsync(_logger, _routeAuthorizationManager, routeContext);
                    if (!authorized) return new RouteResult(RouteResultStatusCode.Unauthorized);

                    //Can deactivate current
                    var canDeactivate =
                        await RouteWorkflow.CanDeactivateCurrentRouteAsync(_logger, _routerService, ViewportOptions, Uri,
                                                                           InitData);
                    if (!canDeactivate) return new RouteResult(RouteResultStatusCode.CanDeactiveFailed);

                    //Activate new route
                    var canActivate = await RouteWorkflow.CanActivateNewRouteAsync(_logger);
                    if (!canActivate) return new RouteResult(RouteResultStatusCode.CanActivateFailed);

                    //Get ViewModel
                    Controller = RouteWorkflow.CreateViewModel(_logger, RouteEntry.Value);
                    await RouteWorkflow.InitViewModel(_logger, InitData, Controller);
                    //Get View
                    View = RouteWorkflow.CreateView(_logger, RouteEntry.Value);
                    RouteWorkflow.AssignDataContext(_logger, View, Controller);

                    //IViewAware context
                    if (Controller is IViewAware viewAware) RouteWorkflow.AssignViewToViewModel(_logger, View, viewAware);

                    //Add View to UI
                    var routeResult = RouteWorkflow.AddViewToUi(_logger, _routerService, this, View);

                    FinishedTime = DateTime.UtcNow;
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
            return $"{RouteName}: {Uri} -> {ViewportOptions}";
        }

        private void LogInfo(string        message)                   => _logger.Information(message);
        private void LogInfo<T>(string     message, T data)           => _logger.Information(message, data);
        private void LogInfo<T, T1>(string message, T data, T1 data1) => _logger.Information(message, data, data1);

        private void LogInfo<T, T1, T2>(string message, T data, T1 data1, T2 data2) =>
            _logger.Information(message, data, data1, data2);

        private void LogWarning(string        message)                   => _logger.Warning(message);
        private void LogWarning<T>(string     message, T data)           => _logger.Warning(message, data);
        private void LogWarning<T, T1>(string message, T data, T1 data1) => _logger.Warning(message, data, data1);

        private void LogError<T>(string message, T data) => _logger.Error((Exception) null, message, data);
    }
}