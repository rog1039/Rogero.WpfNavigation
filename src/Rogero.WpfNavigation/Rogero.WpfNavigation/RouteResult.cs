namespace Rogero.WpfNavigation
{
    public class RouteResult
    {
        public bool Success => StatusCode == RouteResultStatusCode.OK;

        public RouteResultStatusCode StatusCode { get; }

        public RouteResult(RouteResultStatusCode statusCode)
        {
            StatusCode = statusCode;
        }

        public static RouteResult Succeeded { get; } = new RouteResult(RouteResultStatusCode.OK);
    }
}