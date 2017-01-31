﻿using AccountManagement.UI.QueryModels.EventStoreGenerated;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.KeyValueStorage;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.ContainerInstallers
{
    [UsedImplicitly]
    public class AccountManagementQueryModelGeneratingDocumentDbReaderInstaller : IWindsorInstaller
    {
        public static class ComponentKeys
        {
            public const string DocumentDbReader = "AccountManagement.QueryModels.DocumentDbReader";
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<IAccountManagementEventStoreGeneratedQueryModelsReader>()
                    .ImplementedBy<AccountManagementEventStoreGeneratedQueryModelsReader>()
                    .Named(ComponentKeys.DocumentDbReader)
                    .DependsOn(
                        Dependency.OnValue<IDocumentDbSessionInterceptor>(NullOpDocumentDbSessionInterceptor.Instance)
                    )
                    .LifestylePerWebRequest());
        }
    }
}
