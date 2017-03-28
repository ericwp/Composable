﻿using System;
using System.Runtime.Remoting.Messaging;
using System.Transactions;
using Composable.SystemExtensions.Threading;
using Composable.UnitsOfWork;
using JetBrains.Annotations;

namespace Composable.DependencyInjection
{
    public static class PublicUnitOfWorkExtensions
    {
        public static TResult ExecuteUnitOfWork<TResult>(this IServiceLocator me, [InstantHandle]Func<TResult> function)
        {
            TResult result;
            using (var transaction = me.BeginTransactionalUnitOfWorkScope())
            {
                result = function();
                transaction.Commit();
            }
            return result;
        }

        public static void ExecuteUnitOfWork(this IServiceLocator me, [InstantHandle]Action action)
        {
            using (var transaction = me.BeginTransactionalUnitOfWorkScope())
            {
                action();
                transaction.Commit();
            }
        }

    }

    static class UnitOfWorkExtensions
    {
        static TResult ExecuteUnitOfWork<TResult>(this IServiceLocator me, [InstantHandle]Func<TResult> function)
        {
            TResult result;
            using (var transaction = me.BeginTransactionalUnitOfWorkScope())
            {
                result = function();
                transaction.Commit();
            }
            return result;
        }

        static void ExecuteUnitOfWork(this IServiceLocator me, [InstantHandle]Action action)
        {
            using (var transaction = me.BeginTransactionalUnitOfWorkScope())
            {
                action();
                transaction.Commit();
            }
        }

        internal static TResult ExecuteUnitOfWorkInIsolatedScope<TResult>(this IServiceLocator me, [InstantHandle]Func<TResult> function)
        {
            using (me.BeginScope())
            {
                return ExecuteUnitOfWork(me, function);
            }
        }

        internal static void ExecuteUnitOfWorkInIsolatedScope(this IServiceLocator me, [InstantHandle]Action action)
        {
            using (me.BeginScope())
            {
                ExecuteUnitOfWork(me, action);
            }
        }

        internal static TResult ExecuteInIsolatedScope<TResult>(this IServiceLocator me, [InstantHandle]Func<TResult> function)
        {
            using (me.BeginScope())
            {
                return function();
            }
        }

        internal static void ExecuteInIsolatedScope(this IServiceLocator me, [InstantHandle]Action action)
        {
            using (me.BeginScope())
            {
                action();
            }
        }


        public static ITransactionalUnitOfWork BeginTransactionalUnitOfWorkScope(this IServiceLocator @this)
        {
            var currentScope = TransactionalUnitOfWorkScopeBase.CurrentScope;
            if(currentScope == null)
            {
                return TransactionalUnitOfWorkScopeBase.CurrentScope = new TransactionalUnitOfWorkScope(@this);
            }
            return new InnerTransactionalUnitOfWorkScope(TransactionalUnitOfWorkScopeBase.CurrentScope);
        }

        abstract class TransactionalUnitOfWorkScopeBase : ITransactionalUnitOfWork
        {
            public abstract void Dispose();
            public abstract void Commit();
            public abstract bool IsActive { get; }

            internal static TransactionalUnitOfWorkScopeBase CurrentScope
            {
                get
                {
                    var result = (TransactionalUnitOfWorkScopeBase)CallContext.GetData("TransactionalUnitOfWorkScope_Current");
                    if (result != null && result.IsActive)
                    {
                        return result;
                    }
                    return CurrentScope = null;
                }
                set => CallContext.SetData("TransactionalUnitOfWorkScope_Current", value);
            }
        }

        class TransactionalUnitOfWorkScope : TransactionalUnitOfWorkScopeBase, IEnlistmentNotification
        {
            readonly TransactionScope _transactionScopeWeCreatedAndOwn;
            readonly IUnitOfWork _unitOfWork;
            bool _committed;

            public TransactionalUnitOfWorkScope(IServiceLocator container)
            {
                _transactionScopeWeCreatedAndOwn = new TransactionScope();
                try
                {
                    _unitOfWork = new UnitOfWork(container.Resolve<ISingleContextUseGuard>());
                    _unitOfWork.AddParticipants(container.ResolveAll<IUnitOfWorkParticipant>());
                    Transaction.Current.EnlistVolatile(this, EnlistmentOptions.None);
                }
                catch(Exception)
                {
                    _transactionScopeWeCreatedAndOwn.Dispose();//Under no circumstances leave transactions scopes hanging around unmanaged!
                    throw;
                }
            }

            public override void Dispose()
            {
                CurrentScope = null;
                if(!_committed)
                {
                    _unitOfWork.Rollback();
                }
                _transactionScopeWeCreatedAndOwn.Dispose();
            }

            public override void Commit()
            {
                _unitOfWork.Commit();
                _transactionScopeWeCreatedAndOwn.Complete();
                _committed = true;
            }

            public override bool IsActive => !CommitCalled && !RollBackCalled && !InDoubtCalled;

            bool CommitCalled { get; set; }
            bool RollBackCalled { get; set; }
            bool InDoubtCalled { get; set; }
            public void Prepare(PreparingEnlistment preparingEnlistment)
            {
                preparingEnlistment.Prepared();
            }

            public void Commit(Enlistment enlistment)
            {
                CommitCalled = true;
                enlistment.Done();
            }

            public void Rollback(Enlistment enlistment)
            {
                RollBackCalled = true;
                enlistment.Done();
            }

            public void InDoubt(Enlistment enlistment)
            {
                InDoubtCalled = true;
                enlistment.Done();
            }
        }

        class InnerTransactionalUnitOfWorkScope : TransactionalUnitOfWorkScopeBase
        {
            readonly TransactionalUnitOfWorkScopeBase _outer;

            public InnerTransactionalUnitOfWorkScope(TransactionalUnitOfWorkScopeBase outer) => _outer = outer;

            public override void Dispose()
            { }

            public override void Commit()
            { }

            public override bool IsActive => _outer.IsActive;
        }


    }

    interface ITransactionalUnitOfWork : IDisposable
    {
        void Commit();
    }
}