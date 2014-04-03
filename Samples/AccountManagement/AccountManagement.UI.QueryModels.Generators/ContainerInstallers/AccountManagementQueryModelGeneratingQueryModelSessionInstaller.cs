﻿using AccountManagement.UI.QueryModels.EventStore.Generators.Services;
using AccountManagement.UI.QueryModels.Services;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.KeyValueStorage;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.EventStore.Generators.ContainerInstallers
{
    [UsedImplicitly]
    public class AccountManagementQueryModelGeneratingQueryModelSessionInstaller : IWindsorInstaller
    {
        public static class ComponentKeys
        {
            public const string Session = "AccountManagement.QueryModels.Generated.IDocumentDbReader";
            public const string NullOpSessionInterceptor = "AccountManagement.QueryModels.Generated.NullOpSessionInterceptor";
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<IDocumentDbSessionInterceptor>()
                    .Instance(NullOpDocumentDbSessionInterceptor.Instance)
                    .Named(ComponentKeys.NullOpSessionInterceptor)
                    .LifestyleSingleton(),
                Component.For<IAccountManagementQueryModelsReader>()
                    .ImplementedBy<AccountManagementQueryModelGeneratingQueryModelSession>()
                    .Named(ComponentKeys.Session)
                    .LifestylePerWebRequest()
                    .IsDefault()
                );
        }
    }
}
