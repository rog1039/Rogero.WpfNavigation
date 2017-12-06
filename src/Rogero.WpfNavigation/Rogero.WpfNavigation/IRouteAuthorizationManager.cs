using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Threading.Tasks;
using Rogero.Options;

namespace Rogero.WpfNavigation
{
    public interface IRouteAuthorizationManager
    {
        Task<IRouteAuthorizationResult> CheckAuthorization(RoutingContext routingContext);
    }

    public class AlwaysGrantAccessRouteAuthorizationManager : IRouteAuthorizationManager
    {
        public async Task<IRouteAuthorizationResult> CheckAuthorization(RoutingContext routingContext)
        {
            return RouteAuthorizationResult.Granted;
        }
    }

    public class RouteAuthorizationManager : IRouteAuthorizationManager
    {
        private readonly IList<IRouteUriAuthorizer> _routeUriAuthorizers;

        public RouteAuthorizationManager(IList<IRouteUriAuthorizer> routeUriAuthorizers)
        {
            _routeUriAuthorizers = routeUriAuthorizers;
        }

        public async Task<IRouteAuthorizationResult> CheckAuthorization(RoutingContext routingContext)
        {
            var routeAuthTasks = _routeUriAuthorizers
                .Select(z => z.CheckAuthorization(routingContext))
                .ToList();
            var allAuthResults = await Task.WhenAll(routeAuthTasks);
            var authResults = allAuthResults
                .Where(z => z.HasValue)
                .Select(z => z.Value)
                .ToList();

            var notDenied = authResults.All(z => z.RouteAuthorizationStatus != RouteAuthorizationStatus.Denied);
            var authorizedAtleastOnce = authResults.Any(z => z.RouteAuthorizationStatus == RouteAuthorizationStatus.Authorized);
            var authorized = notDenied && authorizedAtleastOnce;

            return authorized
                ? await Task.FromResult(RouteAuthorizationResult.Granted)
                : await Task.FromResult(RouteAuthorizationResult.Denied);
        }
    }

    public interface IRouteUriAuthorizer
    {
        Task<Option<IRouteAuthorizationResult>> CheckAuthorization(RoutingContext routingContext);
    }

    public interface IRoutingContext
    {
        IRouteEntry RouteEntry { get; set; }
        RouteRequest RouteRequest { get; set; }
    }

    public class RoutingContext : IRoutingContext
    {
        public IRouteEntry RouteEntry { get; set; }
        public RouteRequest RouteRequest { get; set; }

        public RoutingContext(IRouteEntry routeEntry, RouteRequest routeRequest)
        {
            RouteEntry = routeEntry;
            RouteRequest = routeRequest;
        }
    }
}
