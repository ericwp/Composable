﻿using System;
using Castle.Windsor;
using Composable.Windsor.Testing;

namespace AccountManagement.UI.Web.Tests
{
    public static class WebTestWiringHelper
    {
        public static WindsorContainer CreateContainer()
        {
            var container = new WindsorContainer();
            CommonWiring(container);
            return container;
        }

        private static void CommonWiring(IWindsorContainer container)
        {
            container.ConfigureWiringForTestsCallBeforeAllOtherWiring();

            ApplicationBootstrapper.ConfigureContainerForTests(container);

            container.ConfigureWiringForTestsCallAfterAllOtherWiring();
        }
    }

    public class TestAuthenticationContext : IAuthenticationContext
    {
        public Guid AccountId { get; set; }
    }
}
