﻿using AccountManagement.UI.QueryModels.EventStoreGenerated;
using Composable.DependencyInjection;

namespace AccountManagement.UI.QueryModels.ContainerInstallers
{
    static class QueryModelGeneratorsInstaller
    {
        internal static void Install(IDependencyInjectionContainer container)
        {
            container.Register(CComponent.For<AccountQueryModelGenerator>()
                .ImplementedBy<AccountQueryModelGenerator>()
                .LifestyleScoped());
        }
    }
}
