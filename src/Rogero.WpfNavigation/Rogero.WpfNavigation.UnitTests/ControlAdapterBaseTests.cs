﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Rogero.Options;
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
            dataContext.ShouldBe(Option<object>.None);
        }
    }

    public class MockControlAdapterBase : ControlViewportAdapterBase
    {
        private UIElement _control;

        public override void AddControl(UIElement control)
        {
            _control = control;
        }

        public override Option<UIElement> ActiveControl => _control;
    }
}
