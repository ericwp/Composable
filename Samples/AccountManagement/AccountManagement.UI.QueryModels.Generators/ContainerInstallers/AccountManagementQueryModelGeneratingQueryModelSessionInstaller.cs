﻿using AccountManagement.UI.QueryModels.Generators.Services;
using AccountManagement.UI.QueryModels.Services;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;

namespace AccountManagement.UI.QueryModels.Generators.ContainerInstallers
{
    public class AccountManagementQueryModelGeneratingQueryModelSessionInstaller : IWindsorInstaller
    {
        public static class ComponentKeys
        {
            public const string Session = "AccountManagement.QueryModels.Generated.IDocumentDbReader";
        }

        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<IAccountManagementQueryModelsReader>()
                    .ImplementedBy<AccountManagementQueryModelGeneratingQueryModelSession>()
                    .Named(ComponentKeys.Session)
                    .LifestylePerWebRequest()
                    .IsDefault()
                );
        }
    }
}