using System.Security.Claims;
using System.Security.Principal;

namespace Rogero.WpfNavigation
{
    public class RouteRequest
    {
        public string Uri { get; set; }
        public object InitData { get; set; }
        public string TargetViewportName { get; set; }
        public ClaimsPrincipal Principal { get; set; }

        public RouteRequest(string uri, object initData, string targetViewportName, ClaimsPrincipal principal)
        {
            Uri = uri;
            InitData = initData;
            TargetViewportName = targetViewportName;
            Principal = principal;
            
        }

        public RouteRequest(){}
    }
}