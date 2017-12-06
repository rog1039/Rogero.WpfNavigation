using Xunit;

namespace Rogero.WpfNavigation.UnitTests
{
    public class RouteAuthorizationResultTests
    {
        [Fact()]
        [Trait("Category", "Instant")]
        public void TestingEqualsOverloadingAndStaticOperatorOverloading()
        {
            IRouteAuthorizationResult granted = RouteAuthorizationResult.Granted;
            Assert.True(granted == RouteAuthorizationResult.Granted);
            Assert.True(granted.Equals(RouteAuthorizationResult.Granted));
            Assert.True(RouteAuthorizationResult.Granted == granted);
            Assert.True(RouteAuthorizationResult.Granted.Equals(granted));
        }
    }
}
