﻿using System;
// ReSharper disable UnusedTypeParameter

namespace Composable.Messaging
{
    public class MessagingApi
    {
        ///<summary>Any object that is used to transfer data from a sender to a receiver through a messaging infrastructure.</summary>
        public interface IMessage {}

        ///<summary>Informs the receiver that something has happened.</summary>
        public interface IEvent : IMessage { }

        /// <summary>Instructs the recevier to perform an action.</summary>
        public interface ICommand : IMessage { }
        public interface ICommand<TResult> : ICommand { }

        public interface IQuery : IMessage { }

        ///<summary>An instructs the receiver to return a resource based upon the data in the query.</summary>
        public interface IQuery<TResult> : MessagingApi.IQuery { }

        public class Local
        {
            public interface IOnlyLocalReceiver {}
            public interface ILocalEvent : IEvent, IOnlyLocalReceiver { }
            public interface ILocalCommand : ICommand, IOnlyLocalReceiver { }
            public interface ILocalCommand<TResult> : ICommand<TResult>, IOnlyLocalReceiver  { }
            public interface ILocalQuery<TResult> : IOnlyLocalReceiver, MessagingApi.IQuery<TResult> { }
        }

        public class Remote
        {
            public interface ISupportRemoteReceiver {}

            public class AtMostOnce
            {
                public interface IAtMostOnceDelivery {}
                public interface IForbidTransactionalSend { }

                public interface IUserInterfaceMessage : IForbidTransactionalSend, IAtMostOnceDelivery, ISupportRemoteReceiver {}
                public interface IUserInterfaceCommand<TResult> : ICommand<TResult>, IUserInterfaceMessage  { }
                public interface IUserInterfaceQuery<TResult> : MessagingApi.IQuery<TResult>, IUserInterfaceMessage { }
            }

            public class ExactlyOnce
            {
                public interface IRequireTransactionalSender : ISupportRemoteReceiver{ }
                public interface IRequireTransactionalReceiver : ISupportRemoteReceiver { }
                public interface IRequireAllOperationsToBeTransactional : IRequireTransactionalSender, IRequireTransactionalReceiver {}

                public interface IProvidesOwnMessageId { Guid MessageId { get; } }
                public interface IExactlyOnceMessage : IRequireAllOperationsToBeTransactional, IProvidesOwnMessageId {}

                public interface IExactlyOnceEvent : IEvent, IExactlyOnceMessage { }
                public interface IExactlyOnceCommand : ICommand, IExactlyOnceMessage { }
                public interface IExactlyOnceCommand<TResult> : ICommand<TResult>, MessagingApi.Remote.ExactlyOnce.IExactlyOnceCommand { }
            }
        }
    }
}
