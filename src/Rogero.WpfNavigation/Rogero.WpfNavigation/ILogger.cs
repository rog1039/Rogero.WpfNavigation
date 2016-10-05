using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;
using Rogero.Options;
using Serilog;

namespace Rogero.WpfNavigation
{
    //public interface ILogger
    //{

    //}

    //public class Logger : ILogger
    //{

    //}
    public class Logging
    {
        public static IDisposable Timing(ILogger logger, string description)
        {
            return FinishTiming.StartTiming(description, logger);
        }

        private class FinishTiming : IDisposable
        {
            public DateTime Start { get; }
            public String Description { get; }
            public ILogger Logger { get; }

            public static FinishTiming StartTiming(string description, ILogger logger)
            {
                var timing = new FinishTiming(DateTime.UtcNow, description, logger);
                return timing;
            }

            public FinishTiming(DateTime start, string description, ILogger logger)
            {
                Start = start;
                Description = description;
                Logger = logger;
                Logger.ForContext("Description", Description).Information("Started " + Description + " at {StartTime}", Start);
            }

            public void Dispose()
            {
                var end = DateTime.UtcNow;
                var elapsed = end - Start;
                var logMessage = $"Finished {Description} in {elapsed:c}.";
                Logger.ForContext("Description", Description)
                    .ForContext("Elapsed", elapsed)
                    .Information(logMessage + " at {EndTime}", end);
            }
        }
    }

    public class RouteWorkflowTask
    {
        private readonly RouterService _routerService;

        public static async Task<RouteResult> Go(string uri, object initData, string viewportName, RouteRegistry routeRegistry, IDictionary<string, IControlViewportAdapter> viewportAdapters, ILogger logger)
        {
            var workflow = new RouteWorkflowTask(uri, initData, viewportName, routeRegistry, viewportAdapters, logger);
            return await workflow.Go();
        }

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
        private readonly IDictionary<string, IControlViewportAdapter> _viewportAdapters;

        private RouteWorkflowTask(string uri, object initData, string viewportName, RouteRegistry routeRegistry, IDictionary<string, IControlViewportAdapter> viewportAdapters, ILogger logger)
        {
            Uri = uri;
            InitData = initData;
            ViewportName = viewportName;
            _viewportAdapters = viewportAdapters;
            _logger = logger
                .ForContext("Uri", uri)
                .ForContext("ViewportName", viewportName)
                .ForContext("InitData", initData)
                .ForContext("RouterServiceId", _routerService.RouterServiceId)
                .ForContext("RoutingWorkflowId", RoutingWorkflowId);
        }

        private RouteWorkflowTask(string uri, object initData, string viewportName, RouterService routerService)
        {
            Uri = uri;
            InitData = initData;
            ViewportName = viewportName;
            _routerService = routerService;
            _logger = _routerService._logger
                .ForContext("Uri", uri)
                .ForContext("ViewportName", viewportName)
                .ForContext("InitData", initData)
                .ForContext("RouterServiceId", _routerService.RouterServiceId)
                .ForContext("RoutingWorkflowId", RoutingWorkflowId);
        }

        private async Task<RouteResult> Go()
        {
            using (var timer = Logging.Timing(_logger, "navigation workflow"))
            {
                try
                {
                    var initDataIsNull = InitData == null ? "with init data" : "without init data";
                    LogInfo("Beginning navigation workflow to {Uri} in viewport {ViewportName} "+initDataIsNull, Uri, ViewportName);

                    var viewVmPair = GetViewVmPair();
                    if (viewVmPair.HasNoValue) return new RouteResult(RouteResultStatusCode.RouteNotFound);

                    var canDeactivate = await CanDeactivateCurrentRoute();
                    if (!canDeactivate) return new RouteResult(RouteResultStatusCode.CanDeactiveFailed);

                    var canActivate = await CanActivateNewRoute();
                    if (!canActivate) return new RouteResult(RouteResultStatusCode.CanActivateFailed);

                    var viewModel = GetViewModel(viewVmPair.Value);
                    InitializeViewModel(viewModel);
                    var view = GetView(viewVmPair.Value);
                    AssignDataContext(view, viewModel);

                    var routeResult = _routerService.AddViewToUi(ViewportName, view);
                    LogInfo("Finished navigation workflow to {Uri} in viewport {ViewportName} " + initDataIsNull, Uri, ViewportName);
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
            var canDeactivate = currentViewModel as ICanDeactivate;
            if (canDeactivate != null)
            {
                LogInfo("Current Viewmodel, {CurrentViewModelType} implements CanDeactivate", canDeactivate.GetType().FullName);
                var canDeactivateResponse = await canDeactivate.CanDeactivate(Uri, InitData);
                LogInfo("CanDeactivate returned {CanDeactivateResponse}", canDeactivateResponse);
                return canDeactivateResponse;
            }
            LogInfo("Current ViewModel, {CurrentViewModelType}, does not implement CanDeactivate", currentViewModel.GetType().FullName);
            return true;
        }

        private Task<bool> CanActivateNewRoute()
        {
            LogInfo("CanActivate always returns true currently.");
            return Task.FromResult(true);
        }

        private object GetViewModel(ViewVmPair value)
        {
            LogInfo("Creating ViewModel.");
            var viewModel = value.CreateViewModel();
            LogInfo("Created viewmodel of type: {ViewModelType}", viewModel.GetType());
            return viewModel;
        }

        private void InitializeViewModel(object viewModel)
        {
            var initMethod = viewModel.GetType().GetMethod("Init");
            if (initMethod == null)
            {
                LogInfo("Viewmodel has no Init method");
                return;
            }
            LogInfo("Initializing viewmodel with InitData");
            initMethod.Invoke(viewModel, new[] { InitData });
            LogInfo("Viewmodel initialized with InitData");
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
            var viewport = _viewportAdapters.TryGetValue(ViewportName);
            if (viewport.HasValue)
            {
                LogInfo("Found viewport, {ViewportName} and adding view to viewport.", ViewportName);
                viewport.Value.AddControl(view);
                LogInfo("View added to viewport");
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

        private void LogError<T>(string message, T data) => _logger.Error((Exception)null, message, data);
    }
}