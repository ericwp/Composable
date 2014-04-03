﻿using AccountManagement.UI.QueryModels.DocumentDB.Readers.Services;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.KeyValueStorage;
using Composable.KeyValueStorage.SqlServer;
using Composable.System.Configuration;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.DocumentDB.Readers.ContainerInstallers
{
    [UsedImplicitly]
    public class AccountManagementQuerymodelsSessionInstaller : IWindsorInstaller
    {
        public static class ComponentKeys
        {
            public const string DocumentDb = "AccountManagement.QueryModels.IDocumentDb";
            public const string InMemoryDocumentDb = "AccountManagement.QueryModels.IDocumentDb.InMemory";
            public const string DocumentDbReader = "AccountManagement.QueryModels.IDocumentDbReader";
        }

        public const string ConnectionStringName = "AccountManagementReadModels";

        public void Install(
            IWindsorContainer container,
            IConfigurationStore store)
        {
            container.Register(
                Component.For<IDocumentDb>()
                    .ImplementedBy<SqlServerDocumentDb>()
                    .DependsOn(new {connectionString = GetConnectionStringFromConfiguration(ConnectionStringName)})
                    .Named(ComponentKeys.DocumentDb)
                    .LifestylePerWebRequest(),
                Component.For<IAccountManagementDocumentDbReader>()
                    .ImplementedBy<AccountManagementDocumentDbQueryModelsReader>()
                    .DependsOn(
                        Dependency.OnComponent(typeof(IDocumentDb), ComponentKeys.DocumentDb),
                        Dependency.OnValue<IDocumentDbSessionInterceptor>(NullOpDocumentDbSessionInterceptor.Instance))
                    .Named(ComponentKeys.DocumentDbReader)
                    .LifestylePerWebRequest()
                );
        }

        private static string GetConnectionStringFromConfiguration(string key)
        {
            return new ConnectionStringConfigurationParameterProvider().GetConnectionString(key).ConnectionString;
        }
    }
}
