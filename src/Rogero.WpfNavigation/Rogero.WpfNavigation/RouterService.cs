using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Windows;
using Rogero.Options;
using Rogero.WpfNavigation.EnumerableTrees;
using Rogero.WpfNavigation.ViewportAdapters;
using Serilog;

namespace Rogero.WpfNavigation
{
    public interface IRouterService
    {
        Guid RouterServiceId { get; }

        Task<RouteResult> RouteAsync(RouteRequest routeRequest);
        Task<RouteResult> RouteAsync(string       uri, object initData, string viewportName, ClaimsPrincipal principal);

        void                           ActivateExistingRouteWorkflow(RouteWorkflowTask routeWorkflowTask);
        IEnumerable<RouteWorkflowTask> GetExistingRouteWorkflowTasks();

        void RegisterViewport(string viewportName, IControlViewportAdapter viewportAdapter);

        Task<bool> CheckForViewport(string viewportName, TimeSpan timeout);

        Option<IControlViewportAdapter> GetControlViewportAdapter(string       viewportName);
        Option<IControlViewportAdapter> GetControlViewportAdapter(RouteRequest routeRequest);

        Option<UIElement> GetActiveControl(string     viewportName);
        Option<object>    GetActiveDataContext(string viewportName);

        void ChangeWindowTitle(string viewportName, string windowTitle);
        void CloseScreen(string       url);
    }

    public class RouterService : IRouterService
    {
        public Guid RouterServiceId { get; } = Guid.NewGuid();

        private readonly ILogger                    _logger;
        private readonly IRouteEntryRegistry        _routeEntryRegistry;
        private readonly IRouteAuthorizationManager _routeAuthorizationManager;

        private readonly IDictionary<string, IControlViewportAdapter> _viewportAdapters =
            new Dictionary<string, IControlViewportAdapter>();

        public RouterService(
            IRouteEntryRegistry        routeEntryRegistry,
            IRouteAuthorizationManager routeAuthorizationManager,
            ILogger                    logger)
        {
            _routeEntryRegistry           = routeEntryRegistry;
            _routeAuthorizationManager    = routeAuthorizationManager;
            InternalLogger.LoggerInstance = logger;
            _logger = logger
                .ForContext("Class",           "RouterService")
                .ForContext("RouterServiceId", RouterServiceId);
            _logger.Information("RouterService created with Id: {RouterServiceId}", RouterServiceId);
        }

        public IEnumerable<RouteWorkflowTask> GetExistingRouteWorkflowTasks()
        {
            return _viewportAdapters
                    .SelectMany(z => z.Value.GetActiveRouteWorkflows())
                ;
        }

        public void RegisterViewport(string viewportName, IControlViewportAdapter viewportAdapter)
        {
            _viewportAdapters.Add(viewportName, viewportAdapter);
            _logger.Information(
                "Viewport registered with name {ViewportName} and Viewport Adapter type {ViewportAdapterType}",
                viewportName, viewportAdapter.GetType().FullName);
        }

        public async Task<RouteResult> RouteAsync(string uri, object initData, string viewportName, ClaimsPrincipal principal)
        {
            var routeRequest = new RouteRequest(uri, initData, viewportName, principal);
            return await RouteAsync(routeRequest);
        }

        public void ActivateExistingRouteWorkflow(RouteWorkflowTask routeWorkflowTask)
        {
            foreach (var viewportAdapter in _viewportAdapters)
            {
                foreach (var activeRouteWorkflow in viewportAdapter.Value.GetActiveRouteWorkflows())
                {
                    if (activeRouteWorkflow == routeWorkflowTask)
                    {
                        viewportAdapter.Value.Activate(activeRouteWorkflow);
                        _logger.Information(
                            $"Found RouteWorkflowTask for URI: {routeWorkflowTask.Uri} with Guid: {activeRouteWorkflow.RoutingWorkflowId}.");
                        return;
                    }
                }
            }

            //If we get here, then no matching RouteWorkflowTask was found in all the viewport adapters.
            _logger.Information($"Existing RouteWorkflowTask for URI: {routeWorkflowTask.Uri} not found.");
        }

        public async Task<RouteResult> RouteAsync(RouteRequest routeRequest)
        {
            var routeWorkflow =
                RouteWorkflowTask.Go(routeRequest, _routeEntryRegistry, _routeAuthorizationManager, this, _logger);
            return await routeWorkflow.RouteResult;
        }

        public Option<IControlViewportAdapter> GetControlViewportAdapter(string viewportName)
        {
            var viewportType = GetViewportType(viewportName);
            if (viewportType == ViewportType.NewWindow)
            {
                var window          = new Window();
                var viewportAdapter = new WindowViewportAdapter(window);
                return viewportAdapter;
            }
            else if (viewportType == ViewportType.Dialog) { }
            else if (viewportType == ViewportType.NormalViewport)
            {
                return _viewportAdapters.TryGetValue(viewportName);
            }

            throw new NotImplementedException();
        }

        public Option<IControlViewportAdapter> GetControlViewportAdapter(RouteRequest routeRequest)
        {
            return GetControlViewportAdapter(routeRequest.TargetViewportName);
        }

        public static ViewportType GetViewportType(string viewportName)
        {
            return ViewportNames.GetViewportTypeFromName(viewportName);
        }

        public Option<UIElement> GetActiveControl(string viewportName)
        {
            var viewportAdapter = GetControlViewportAdapter(viewportName);
            return viewportAdapter.HasNoValue
                ? Option<UIElement>.None
                : viewportAdapter.Value.ActiveControl;
        }

        public Option<object> GetActiveDataContext(string viewportName)
        {
            var viewportAdapter = GetControlViewportAdapter(viewportName);
            return viewportAdapter.HasNoValue
                ? Option<UIElement>.None
                : viewportAdapter.Value.ActiveDataContext;
        }

        public void ChangeWindowTitle(string viewportName, string windowTitle)
        {
            try
            {
                var viewportOption = GetControlViewportAdapter(viewportName);
                if (viewportOption.HasNoValue)
                    throw new InvalidOperationException($"No viewport could be found with name {viewportName}");

                var viewport     = viewportOption.Value;
                var associatedUI = viewport.ViewportUIElement;
                var parentWindow = associatedUI.FindParentWindow();
                parentWindow.DoIfValue(window => window.Title = windowTitle);
            }
            catch (Exception e)
            {
                _logger.Error(e, "Error updating the window title.");
            }
        }

        public void CloseScreen(string url)
        {
            var adapterAndWorkflows = from adapter in _viewportAdapters.Values
                                      from workflow in adapter.GetActiveRouteWorkflows()
                                      where workflow.Uri == url
                                      select (adapter, workflow);
            foreach (var adapterAndWorkflow in adapterAndWorkflows)
            {
                adapterAndWorkflow.adapter.CloseScreen(adapterAndWorkflow.workflow);
            }
        }

        public async Task<bool> CheckForViewport(string viewportName, TimeSpan timeout)
        {
            async Task<bool> FindViewport()
            {
                var startTime      = DateTime.UtcNow;
                var viewportExists = false;

                TimeSpan Elapsed() => DateTime.UtcNow - startTime;

                bool TimedOut() => Elapsed() > timeout;

                bool ViewportFound()
                {
                    viewportExists = DoesViewportExist(viewportName);
                    return viewportExists;
                }

                while (!TimedOut() && !ViewportFound())
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(50));
                }

                return viewportExists;
            }

            var findViewportTask = Task.Run(FindViewport);
            return await findViewportTask;
        }

        public bool DoesViewportExist(string viewportName) => _viewportAdapters.ContainsKey(viewportName);

        public IList<RouteWorkflowTask> GetActiveRouteWorkflows()
        {
            var allActiveRouteWorkflows = _viewportAdapters.Values
                .SelectMany(z => z.GetActiveRouteWorkflows())
                .ToList();
            return allActiveRouteWorkflows;
        }
    }
}