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
        Task<IRouteAuthorizationResult> CheckAuthorization(RouteRequest routeRequest, RoutingContext routingContext);
    }

    public class RouteAuthorizationManager : IRouteAuthorizationManager
    {
        private readonly IList<IRouteUriAuthorizer> _routeUriAuthorizers;

        public RouteAuthorizationManager(IList<IRouteUriAuthorizer> routeUriAuthorizers)
        {
            _routeUriAuthorizers = routeUriAuthorizers;
        }

        public async Task<IRouteAuthorizationResult> CheckAuthorization(RouteRequest routeRequest, RoutingContext routingContext)
        {
            var results = _routeUriAuthorizers
                .Select(z => z.CheckAuthorization(routeRequest, routingContext))
                .Where(z => z.HasValue)
                .Select(z => z.Value)
                .ToList();

            var notDenied = results.All(z => z.RouteAuthorizationStatus != RouteAuthorizationStatus.Denied);
            var authorizedAtleastOnce = results.Any(z => z.RouteAuthorizationStatus == RouteAuthorizationStatus.Authorized);
            var authorized = notDenied && authorizedAtleastOnce;

            return authorized
                ? await Task.FromResult(RouteAuthorizationResult.Granted)
                : await Task.FromResult(RouteAuthorizationResult.Denied);
        }
    }

    public interface IRouteUriAuthorizer
    {
        Option<IRouteAuthorizationResult> CheckAuthorization(RouteRequest routeRequest, RoutingContext routingContext);
    }

    public interface IRoutingContext
    {
        IPrincipal Principal { get; set; }
    }

    public class RoutingContext : IRoutingContext
    {
        public IPrincipal Principal { get; set; }
    }
}
