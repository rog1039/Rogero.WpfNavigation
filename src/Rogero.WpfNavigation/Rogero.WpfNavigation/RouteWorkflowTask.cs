using System;
using System.Threading.Tasks;
using System.Windows;
using Optional;
using Optional.Linq;
using Optional.Unsafe;
using Rogero.WpfNavigation.ExtensionMethods;
using Serilog;

namespace Rogero.WpfNavigation
{
    public class RouteWorkflowTask
    {
        public static RouteWorkflowTask Go(
            RouteRequest routeRequest,
            IRouteEntryRegistry routeEntryRegistry,
            IRouteAuthorizationManager routeAuthorizationManager,
            IRouterService routerService,
            ILogger logger)
        {
            var workflow = new RouteWorkflowTask(routeRequest, routeEntryRegistry, routeAuthorizationManager,
                routerService,
                logger);
            workflow.RouteResult = workflow.GoAsync();
            return workflow;
        }

        public string Uri => _routeRequest.Uri;
        public object InitData => _routeRequest.InitData;
        public ViewportOptions ViewportOptions => _routeRequest.ViewportOptions;
        public Guid RouteRequestId => _routeRequest.RouteRequestId;
        public Option<string> RouteName => RouteEntryOption.Select(routeEntry => routeEntry.Name);

        public Guid RoutingWorkflowId { get; } = Guid.NewGuid();
        public DateTime StartedTime { get; } = DateTime.UtcNow;
        public DateTime FinishedTime { get; private set; }
        public Option<IRouteEntry> RouteEntryOption { get; private set; }
        public Task<RouteResult> RouteResult { get; private set; }

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
                .ForContext(SerilogConstants.Serilog_SourceContext_Name, nameof(RouteWorkflowTask))
                .ForContext("Uri", Uri)
                .ForContext("ViewportName", ViewportOptions.ToString())
                .ForContext("InitData", InitData)
                .ForContext("RouterServiceId", _routerService.RouterServiceId)
                .ForContext("RoutingWorkflowId", RoutingWorkflowId);
        }

        internal async Task<RouteResult> GoAsync()
        {
            var initDataIsNull = InitData != null ? " with init data" : " without init data";
            using var timer = PerformanceTimer.Start(_logger,
                $"Routing operation to {Uri} in Viewport {ViewportOptions.ToString() + initDataIsNull}");

            try
            {
                RouteEntryOption = RouteWorkflow.GetRouteEntry(_logger, _routeEntryRegistry, Uri);
                timer.Checkpoint("Route entry retrieved.");

                var routeResult = await RouteEntryOption.Match(
                    some: async routeEntry =>
                    {
                        //Check authorization
                        var routeContext = new RoutingContext(routeEntry, _routeRequest);
                        var authorized =
                            await RouteWorkflow.CheckRouteAuthorizationAsync(
                                _logger, _routeAuthorizationManager, routeContext);

                        timer.Checkpoint("Route auth checked.");
                        if (!authorized) return new RouteResult(RouteResultStatusCode.Unauthorized);

                        //Can deactivate current
                        var canDeactivate =
                            await RouteWorkflow.CanDeactivateCurrentRouteAsync(
                                _logger, _routerService, ViewportOptions, Uri,
                                InitData);
                        timer.Checkpoint("CanDeactivate checked.");
                        if (!canDeactivate) return new RouteResult(RouteResultStatusCode.CanDeactiveFailed);

                        //Activate new route
                        var canActivate = await RouteWorkflow.CanActivateNewRouteAsync(_logger);
                        timer.Checkpoint("Activate checked.");
                        if (!canActivate) return new RouteResult(RouteResultStatusCode.CanActivateFailed);

                        //Get ViewModel
                        Controller = RouteWorkflow.CreateViewModel(_logger, routeEntry);
                        timer.Checkpoint("Viewmodel created");
                        await RouteWorkflow.InitViewModel(_logger, InitData, Controller);
                        timer.Checkpoint("Viewmodel inited.");
                        //Get View
                        View = RouteWorkflow.CreateView(_logger, routeEntry);
                        timer.Checkpoint("View created.");
                        RouteWorkflow.AssignDataContext(_logger, View, Controller);

                        //IViewAware context
                        if (Controller is IViewAware viewAware)
                        {
                            RouteWorkflow.AssignViewToViewModel(_logger, View, viewAware);
                            timer.Checkpoint("ViewModel is view aware, so view assigned to view model");
                        }

                        //Add View to UI
                        var routeResult = RouteWorkflow.AddViewToUi(_logger, _routerService, this, View);
                        timer.Checkpoint("View added to UI.K");
                        return routeResult;
                    },
                    none: async () => new RouteResult(RouteResultStatusCode.RouteNotFound));

                FinishedTime = DateTime.UtcNow;

                return routeResult;
            }
            catch (Exception e)
            {
                _logger.Error(e, "Exception during RouteWorkflowTask.Go() method.");
                throw;
            }
        }

        public override string ToString()
        {
            return $"{RouteName}: {Uri} -> {ViewportOptions}";
        }

        private void LogInfo(string message) => _logger.Information(message);
        private void LogInfo<T>(string message, T data) => _logger.Information(message, data);
        private void LogInfo<T, T1>(string message, T data, T1 data1) => _logger.Information(message, data, data1);

        private void LogInfo<T, T1, T2>(string message, T data, T1 data1, T2 data2) =>
            _logger.Information(message, data, data1, data2);

        private void LogWarning(string message) => _logger.Warning(message);
        private void LogWarning<T>(string message, T data) => _logger.Warning(message, data);
        private void LogWarning<T, T1>(string message, T data, T1 data1) => _logger.Warning(message, data, data1);

        private void LogError<T>(string message, T data) => _logger.Error((Exception) null, message, data);
    }
}