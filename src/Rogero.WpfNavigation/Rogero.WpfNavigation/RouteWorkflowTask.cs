using System;
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
                    InitializeViewModel(viewModel);
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
}