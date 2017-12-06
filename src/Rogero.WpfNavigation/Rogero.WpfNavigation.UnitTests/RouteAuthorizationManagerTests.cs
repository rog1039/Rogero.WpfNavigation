﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Moq;
using Ploeh.AutoFixture;
using Rogero.AutoFixture.Helpers;
using Rogero.Options;
using Rogero.WpfNavigation.UnitTests.Extensions;
using Shouldly;
using Xunit;

namespace Rogero.WpfNavigation.UnitTests
{
    public class RouteAuthorizationManagerTests : AutoFixtureBase
    {
        private readonly IRouteUriAuthorizer _alwaysGrantAuthorizer;
        private readonly IRouteUriAuthorizer _alwaysDenyAuthorizer;
        private readonly IRouteUriAuthorizer _alwaysNotDeterminedAuthorizer;

        public RouteAuthorizationManagerTests()
        {
            var alwaysGrantMoq = new Mock<IRouteUriAuthorizer>();
            alwaysGrantMoq
                .Setup(z => z.CheckAuthorization(It.IsAny<RoutingContext>()))
                .Returns(() => RouteAuthorizationResult.Granted.ToOption().ToTask());
            _alwaysGrantAuthorizer = alwaysGrantMoq.Object;

            var alwaysDenyMoq = new Mock<IRouteUriAuthorizer>();
            alwaysDenyMoq
                .Setup(z => z.CheckAuthorization(It.IsAny<RoutingContext>()))
                .Returns(() => RouteAuthorizationResult.Denied.ToOption().ToTask());
            _alwaysDenyAuthorizer = alwaysDenyMoq.Object;

            var alwaysNotDeterminedMoq = new Mock<IRouteUriAuthorizer>();
            alwaysNotDeterminedMoq
                .Setup(z => z.CheckAuthorization(It.IsAny<RoutingContext>()))
                .Returns(() => RouteAuthorizationResult.NotDetermined.ToOption().ToTask());
            _alwaysNotDeterminedAuthorizer = alwaysNotDeterminedMoq.Object;
        }

        [Fact()]
        public async Task GrantTestAsync()
        {
            var manager = new RouteAuthorizationManager(_alwaysGrantAuthorizer.MakeList());
            var result = await manager.CheckAuthorization(_fixture.Create<RoutingContext>());
            Console.WriteLine(result.RouteAuthorizationStatus);
            result.ShouldBe(RouteAuthorizationResult.Granted);
        }

        [Fact()]
        public async Task DeniedTestAsync()
        {
            var manager = new RouteAuthorizationManager(_alwaysDenyAuthorizer.MakeList());
            var result = await manager.CheckAuthorization(_fixture.Create<RoutingContext>());
            Console.WriteLine(result.RouteAuthorizationStatus);
            result.ShouldBe(RouteAuthorizationResult.Denied);
        }

        [Fact()]
        public async Task GrantNotDeterminedAsync()
        {
            var manager = new RouteAuthorizationManager(_alwaysNotDeterminedAuthorizer.MakeList());
            var result = await manager.CheckAuthorization(_fixture.Create<RoutingContext>());
            Console.WriteLine(result.RouteAuthorizationStatus);
            result.ShouldBe(RouteAuthorizationResult.Denied);
        }

        [Fact()]
        public async Task DeniedAndGrantedTestAsync()
        {
            var authorizers = new List<IRouteUriAuthorizer>() {_alwaysGrantAuthorizer, _alwaysDenyAuthorizer};
            var manager = new RouteAuthorizationManager(authorizers);
            var result = await manager.CheckAuthorization(_fixture.Create<RoutingContext>());
            Console.WriteLine(result.RouteAuthorizationStatus);
            result.ShouldBe(RouteAuthorizationResult.Denied);
        }
    }
}