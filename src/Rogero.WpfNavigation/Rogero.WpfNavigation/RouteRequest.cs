using System;
using System.Security.Claims;

namespace Rogero.WpfNavigation
{
    public class RouteRequest
    {
        public string          Uri             { get; set; }
        public object          InitData        { get; set; }
        public ViewportOptions ViewportOptions { get; set; }
        public ClaimsPrincipal Principal       { get; set; }

        public Guid RouteRequestId { get; set; } = Guid.NewGuid();

        public RouteRequest(string uri, object initData, ViewportOptions viewportOptions, ClaimsPrincipal principal)
        {
            Uri             = uri;
            InitData        = initData;
            ViewportOptions = viewportOptions;
            Principal       = principal;
        }

        public RouteRequest() { }
    }
}