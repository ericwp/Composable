﻿
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public static class NavigationSpecificationMessageExtensions
    {
        public static NavigationSpecification<TResult> Post<TResult>(this ITransactionalExactlyOnceDeliveryCommand<TResult> command) => NavigationSpecification.Post(command);

        public static NavigationSpecification Post(this ITransactionalExactlyOnceDeliveryCommand command) => NavigationSpecification.Post(command);

        public static NavigationSpecification<TResult> Get<TResult>(this IQuery<TResult> query) => NavigationSpecification.Get(query);


        public static TResult PostOn<TResult>(this ITransactionalExactlyOnceDeliveryCommand<TResult> command, IServiceBusSession bus) => NavigationSpecification.Post(command).ExecuteOn(bus);

        public static void PostOn(this ITransactionalExactlyOnceDeliveryCommand command, IServiceBusSession bus) => NavigationSpecification.Post(command).ExecuteOn(bus);

        public static TResult GetOn<TResult>(this IQuery<TResult> query, IServiceBusSession bus) => NavigationSpecification.Get(query).ExecuteOn(bus);
    }
}
