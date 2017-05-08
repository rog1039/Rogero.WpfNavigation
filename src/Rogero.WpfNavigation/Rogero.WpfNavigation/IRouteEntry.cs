using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using Rogero.Options;

namespace Rogero.WpfNavigation
{
    public interface IRouteEntry
    {
        string Name { get; }
        string Uri { get; }
        Type Controller { get; }
        Type View { get; }
    }

    public interface IRouteAuthorizationManager
    {
        Task<IRouteAuthorizationResult> CheckAuthorization(string uri, IPrincipal principal);
    }

    public class RouteAuthorizationManager : IRouteAuthorizationManager
    {
        private readonly IList<IRouteUriAuthorizer> _routeUriAuthorizers;

        public RouteAuthorizationManager(IList<IRouteUriAuthorizer> routeUriAuthorizers)
        {
            _routeUriAuthorizers = routeUriAuthorizers;
        }

        public async Task<IRouteAuthorizationResult> CheckAuthorization(string uri, IPrincipal principal)
        {
            var results = _routeUriAuthorizers
                .Select(z => z.CheckAuthorization(uri, principal))
                .Where(z => z.HasValue)
                .Select(z => z.Value)
                .ToList();

            if (results.Count > 0 && results.All(z => z.IsAuthorized)) return await Task.FromResult(RouteAuthorizationResult.Granted);
            return await Task.FromResult(RouteAuthorizationResult.Denied);
        }
    }

    public interface IRouteUriAuthorizer
    {
        Option<IRouteAuthorizationResult> CheckAuthorization(string uri, IPrincipal principal);
    }
}
