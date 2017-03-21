﻿using AccountManagement.Domain.API;
using AccountManagement.Domain.Events.PropertyUpdated;
using AccountManagement.Domain.QueryModels.Updaters;
using Castle.MicroKernel.Registration;
using Castle.MicroKernel.SubSystems.Configuration;
using Castle.Windsor;
using Composable.Messaging;
using Composable.Messaging.Buses;
using JetBrains.Annotations;

namespace AccountManagement.Domain.ContainerInstallers
{
    [UsedImplicitly] public class MessageHandlersInstaller : IWindsorInstaller
    {
        public void Install(IWindsorContainer container, IConfigurationStore store)
        {
            container.Register(
                Component.For<EmailToAccountMapQueryModelUpdater>().LifestyleScoped()
                );

            container.Resolve<IMessageHandlerRegistrar>()
                     .ForEvent<IAccountEmailPropertyUpdatedEvent>(@event => container.Resolve<EmailToAccountMapQueryModelUpdater>()
                                                                                     .Handle(@event))
                     .ForQuery<IQuery<StartResource>, StartResource>(query => new StartResource());
        }
    }
}