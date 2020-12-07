using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Ink;
using Optional;
using Optional.Unsafe;
using Rogero.WpfNavigation.ExtensionMethods;
using Serilog;

namespace Rogero.WpfNavigation
{
    public static class RouteWorkflow
    {
        public static Option<IRouteEntry> GetRouteEntry(ILogger logger, IRouteEntryRegistry routeEntryRegistry, string uri)
        {
            logger.Information($"Finding RouteEntry for uri: {uri}");
            var routeEntryMaybe = routeEntryRegistry.GetRouteEntry(uri);

            routeEntryMaybe.Match(
                routeEntry => logger.Information("Found RouteEntry, ViewType: {ViewType}", routeEntry.ViewType),
                () => logger.Warning("Did not find RouteEntry")
            );
            
            return routeEntryMaybe;
        }

        public static void AssignViewToViewModel(ILogger logger, UIElement view, IViewAware viewAware)
        {
            logger.Information("ViewModel {ViewModelType} is IViewAware so calling LoadView() with the View {ViewType}",
                               viewAware.GetType().FullName,
                               view.GetType().FullName);

            viewAware.LoadView(view);
        }

        public static object CreateViewModel(ILogger logger, IRouteEntry routeEntry)
        {
            logger.Information("Creating viewmodel of type {ViewModelType}", routeEntry.ViewModelType);

            var viewModel = routeEntry.CreateViewModel();
            logger.Information("Created viewmodel of type: {ViewModelType}", routeEntry.ViewModelType);
            return viewModel;
        }

        public static UIElement CreateView(ILogger logger, IRouteEntry routeEntry)
        {
            logger.Information("Creating the view.");

            var uiElement = routeEntry.CreateView();
            logger.Information("View, {ViewType}, created.", uiElement.GetType());
            return uiElement;
        }

        /// <summary>
        /// Checks if the user has authorization to go to the target route.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="routeAuthorizationManager"></param>
        /// <param name="routingContext"></param>
        /// <returns></returns>
        public static async Task<bool> CheckRouteAuthorizationAsync(
            ILogger                    logger,
            IRouteAuthorizationManager routeAuthorizationManager,
            RoutingContext             routingContext)
        {
            try
            {
                var routeAuthResult = await routeAuthorizationManager.CheckAuthorization(routingContext);
                var granted         = routeAuthResult.Equals(RouteAuthorizationResult.Granted);

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

        /// <summary>
        /// Initializes the ViewModel, with InitData if possible. If the ViewModel does not have an Init(), then does nothing.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="initData"></param>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static async Task InitViewModel(
            ILogger logger,
            object  initData,
            object  viewModel
        )
        {
            var initMethod = GetViewModelInitMethod(viewModel);
            if (initMethod is null)
            {
                logger.Information("Viewmodel has no Init method");
                return;
            }

            var parameterCount = initMethod.GetParameters().Length;
            switch (parameterCount)
            {
                case 0:
                    logger.Information("Initializing viewmodel with no parameters");
                    if (initData != null)
                        logger.Warning("Viewmodel Init method has no parameters, but InitData was passed to this route request.");

                    var result0 = initMethod.Invoke(viewModel, new object[] { });
                    logger.Information("Viewmodel initialization with no parameters called");

                    await result0.AwaitIfNecessary();
                    logger.Information("Viewmodel initialization returned");
                    return;

                case 1:
                    logger.Information("Initializing viewmodel with InitData of type {InitDataType}", initData?.GetType());

                    if (initData == null)
                        logger.Warning("Passing null to a new viewmodel {ViewModelType} that has a paramter in the Init method",
                                       viewModel.GetType());

                    var result = initMethod.Invoke(viewModel, new[] {initData});
                    logger.Information("Viewmodel initialized with InitData");

                    await result.AwaitIfNecessary();
                    logger.Information("Viewmodel initialization with InitData returned");
                    return;

                case int count when count > 1:

                    var exception = new NotImplementedException();
                    logger.Error("ViewModel init method has more than 1 parameter and this is not supported at this time.",
                                 exception);

                    throw exception;
            }
        }

        /// <summary>
        /// Gets the ViewModel Init() method. Returns only methods that have 0 or 1 parameters and if both exist, returns the
        /// function with 1 parameter over the zero parameter method.
        /// </summary>
        /// <param name="viewModel"></param>
        /// <returns></returns>
        private static MethodInfo GetViewModelInitMethod(object viewModel)
        {
            var initMethods = viewModel.GetType()
                .GetMethods()
                .Where(z => z.Name.StartsWith("Init"))
                .Where(z => z.GetParameters().Length < 2)
                .OrderByDescending(z => z.GetParameters().Length)
                .ToList();

            return initMethods.FirstOrDefault();
        }


        /// <summary>
        /// Attempts to assign the ViewModel to the View's DataContext property.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="view"></param>
        /// <param name="viewModel"></param>
        public static void AssignDataContext(ILogger logger, object view, object viewModel)
        {
            switch (view, viewModel)
            {
                case (FrameworkElement frameworkElement, null):
                    logger.Information("View was a FrameworkElement but the ViewModel was null, " +
                                       "so nothing was set as the datacontext.");
                    return;

                case (FrameworkElement frameworkElement2, _):
                    logger.Information("Assigning viewmodel ({ViewModelType}) to view ({ViewType}) DataContext",
                                       viewModel.GetType(),
                                       frameworkElement2.GetType());
                    frameworkElement2.DataContext = viewModel;
                    logger.Information("Assigned viewmodel to view DataContext.");
                    return;

                case (_, {}):
                    logger.Warning(
                        "Viewmodel of type {ViewModelType} was created but was not assigned to " +
                        "the View of type {ViewType} since the view does not have a DataContext property",
                        viewModel.GetType(),
                        view.GetType());
                    return;

                case (_, null):
                    logger.Information(
                        "The viewmodel was null which is good because the view {ViewType} does " +
                        "not derive from FrameworkElement and does not have a DataContext property to assign to anyway.",
                        view.GetType());
                    return;
            }
        }

        /// <summary>
        /// Returns whether or not the CanDeactivate is allowed. If the existing ViewModel does not exist or does not support
        /// ICanDeactivate, then returns true. Otherwise, returns the result of ViewModel.CanDeactivate().
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="routerService"></param>
        /// <param name="viewportOptions"></param>
        /// <param name="uri"></param>
        /// <param name="initData"></param>
        /// <returns></returns>
        public static async Task<bool> CanDeactivateCurrentRouteAsync(ILogger         logger,
                                                                      IRouterService  routerService,
                                                                      ViewportOptions viewportOptions,
                                                                      string          uri,
                                                                      object          initData)
        {
            var viewportExists = CheckViewportNameExists(viewportOptions, routerService);
            if (!viewportExists)
            {
                logger.Information($"{nameof(CanDeactivateCurrentRouteAsync)} will return true because no" +
                                   $" viewport was found matching {viewportOptions}.");
                return true;
            }

            var currentViewModelOption = routerService.GetActiveDataContext(viewportOptions);

            return await currentViewModelOption
                .Match(async currentViewModel =>
                       {
                           switch (currentViewModel)
                           {
                               case ICanDeactivate iCanDeactivate:
                               {
                                   logger.Information(
                                       "Current viewmodel, {CurrentViewModelType} implements CanDeactivate",
                                       iCanDeactivate.GetType().FullName);

                                   var canDeactivateResponse =
                                       await iCanDeactivate.CanDeactivate(uri, initData);

                                   logger.Information(
                                       "CanDeactivate returned {CanDeactivateResponse}",
                                       canDeactivateResponse);
                                   return canDeactivateResponse;
                               }
                               default:
                               {
                                   logger.Information(
                                       "Current viewmodel, {CurrentViewModelType}, does not implement CanDeactivate",
                                       currentViewModel.GetType().FullName);
                                   return true;
                               }
                           }
                       },
                       async () =>
                       {
                           logger.Information(
                               "No current viewmodel to call CanDeactivate upon");
                           return true;
                       });
        }

        /// <summary>
        /// Tell us if the ViewportName exists in this router service. If the viewport is for a new window or dialog,
        /// then this automatically returns false. Will only return true for "normal" viewport names that exist.
        /// </summary>
        /// <param name="viewportName"></param>
        /// <param name="routerService"></param>
        /// <returns></returns>
        private static bool CheckViewportNameExists(ViewportOptions viewportOptions, IRouterService routerService)
        {
            switch (viewportOptions)
            {
                case NewWindowViewportOptions newWindowViewportOptions:
                    return false;
                case StandardViewportOptions standardViewportOptions:
                    var controlViewportAdapter = routerService.GetExistingStandardViewportAdapter(standardViewportOptions);
                    return controlViewportAdapter != null;
                default:
                    throw new ArgumentOutOfRangeException(nameof(viewportOptions));
            }
        }

        public static async Task<bool> CanActivateNewRouteAsync(ILogger logger)
        {
            logger.Information("CanActivate always returns true currently.");
            return true;
        }

        /// <summary>
        /// Attempts to add the newly created View to the proper target Viewport.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="_routerService"></param>
        /// <param name="routeWorkflowTask"></param>
        /// <param name="view"></param>
        /// <returns></returns>
        public static RouteResult AddViewToUi(ILogger           logger,
                                              IRouterService    _routerService,
                                              RouteWorkflowTask routeWorkflowTask,
                                              UIElement         view)
        {
            var viewportOptions = routeWorkflowTask.ViewportOptions;
            var viewportAdapterOption   = _routerService.GetControlViewportAdapter(viewportOptions);

            switch (viewportAdapterOption.ValueOrDefault())
            {
                case null:
                    logger.Error("No viewport found with specified viewport name, {ViewportName}",
                                 viewportOptions.ToString());
                    return new RouteResult(RouteResultStatusCode.NoViewportFound);

                case { } viewportAdapter:
                    logger.Information("Found target viewport, {ViewportName}, of type {ViewportType}. Adding view to viewport.",
                                       viewportOptions.ToString(),
                                       viewportAdapter.GetType());

                    viewportAdapter.AddControl(view, routeWorkflowTask);

                    logger.Information("View {ViewType} added to viewport {ViewportName}, type: {ViewportType}",
                                       view.GetType(),
                                       viewportOptions.ToString(),
                                       viewportAdapter.GetType());
                    return RouteResult.Succeeded;
            }
        }
    }
}