namespace Rogero.WpfNavigation
{
    public interface IRouteAuthorizationResult
    {
        bool IsAuthorized { get; }
        bool NotAuthorized { get; }
    }

    public class RouteAuthorizationResult : IRouteAuthorizationResult
    {
        public bool IsAuthorized { get; }
        public bool NotAuthorized => !IsAuthorized;

        public RouteAuthorizationResult(bool isAuthorized)
        {
            IsAuthorized = isAuthorized;
        }

        public static RouteAuthorizationResult Denied { get; } = new RouteAuthorizationResult(false);
        public static RouteAuthorizationResult Granted { get; } = new RouteAuthorizationResult(true);
    }
}