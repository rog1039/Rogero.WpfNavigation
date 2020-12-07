using System.Collections.Generic;
using System.Windows;
using Optional;
using Rogero.WpfNavigation.ViewportAdapters;
using Shouldly;
using Xunit;

namespace Rogero.WpfNavigation.UnitTests
{
    public class ControlAdapterBaseTests
    {
        [Fact()]
        [Trait("Category", "Instant")]
        public void ControlIsNull()
        {
            var sut = new MockControlAdapterBase();
            var dataContext = sut.ActiveDataContext;
            dataContext.ShouldBe(Option.None<object>());
        }
    }

    public class MockControlAdapterBase : ControlViewportAdapterBase
    {
        private UIElement _control;

        public override void AddControl(UIElement control, RouteWorkflowTask routeWorkflowTask)
        {
            throw new System.NotImplementedException();
        }

        public override Option<UIElement> ActiveControl => _control.SomeNotNull();
        public override IList<RouteWorkflowTask> GetActiveRouteWorkflows()
        {
            throw new System.NotImplementedException();
        }

        public override void Activate(RouteWorkflowTask activeRouteWorkflow)
        {
            throw new System.NotImplementedException();
        }

        public override void CloseScreen(RouteWorkflowTask workflow)
        {
            throw new System.NotImplementedException();
        }
    }
}
