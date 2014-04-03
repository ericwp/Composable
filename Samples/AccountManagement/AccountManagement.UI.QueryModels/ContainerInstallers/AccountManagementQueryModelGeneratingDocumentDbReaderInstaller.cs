﻿using AccountManagement.UI.QueryModels.Generators;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.KeyValueStorage;

namespace AccountManagement.UI.QueryModels.ContainerInstallers
{
    public class AccountManagementQueryModelGeneratingDocumentDbReaderInstaller : IWindsorInstaller
    {
        public static class ComponentKeys
        {
            public const string DocumentDbReader = "AccountManagement.QueryModels.DocumentDbReader";
        }
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<AccountManagementQueryModelGeneratingDocumentDbReader>()
                    .ImplementedBy<AccountManagementQueryModelGeneratingDocumentDbReader>()
                    .Named(ComponentKeys.DocumentDbReader)
                    .DependsOn(
                        Dependency.OnValue<IDocumentDbSessionInterceptor>(NullOpDocumentDbSessionInterceptor.Instance)
                    )
                    .LifestylePerWebRequest());
        }
    }
}