﻿using System;
using System.Threading.Tasks;
using Composable.Messaging.Buses;

namespace Composable.Messaging
{
    public abstract partial class RemoteNavigationSpecification<TResult>
    {
        internal static class Remote
        {
            internal class StartQuery : RemoteNavigationSpecification<TResult>
            {
                readonly BusApi.Remote.NonTransactional.IQuery<TResult> _start;

                internal StartQuery(BusApi.Remote.NonTransactional.IQuery<TResult> start) => _start = start;

                public override TResult ExecuteRemoteOn(IUIInteractionApiBrowser busSession) => busSession.GetRemote(_start);
                public override Task<TResult> ExecuteRemoteAsyncOn(IUIInteractionApiBrowser busSession) => busSession.GetRemoteAsync(_start);
            }

            internal class StartCommand : RemoteNavigationSpecification<TResult>
            {
                readonly BusApi.Remote.AtMostOnce.ICommand<TResult> _start;

                internal StartCommand(BusApi.Remote.AtMostOnce.ICommand<TResult> start) => _start = start;

                public override TResult ExecuteRemoteOn(IUIInteractionApiBrowser busSession) => busSession.PostRemote(_start);
                public override Task<TResult> ExecuteRemoteAsyncOn(IUIInteractionApiBrowser busSession) => busSession.PostRemoteAsync(_start);
            }

            internal class ContinuationQuery<TPrevious> : RemoteNavigationSpecification<TResult>
            {
                readonly RemoteNavigationSpecification<TPrevious> _previous;
                readonly Func<TPrevious, BusApi.Remote.NonTransactional.IQuery<TResult>> _nextQuery;

                internal ContinuationQuery(RemoteNavigationSpecification<TPrevious> previous, Func<TPrevious, BusApi.Remote.NonTransactional.IQuery<TResult>> nextQuery)
                {
                    _previous = previous;
                    _nextQuery = nextQuery;
                }

                public override TResult ExecuteRemoteOn(IUIInteractionApiBrowser busSession)
                {
                    var previousResult = _previous.ExecuteRemoteOn(busSession);
                    var currentQuery = _nextQuery(previousResult);
                    return busSession.GetRemote(currentQuery);
                }

                public override async Task<TResult> ExecuteRemoteAsyncOn(IUIInteractionApiBrowser busSession)
                {
                    var previousResult = await _previous.ExecuteRemoteAsyncOn(busSession);
                    var currentQuery = _nextQuery(previousResult);
                    return await busSession.GetRemoteAsync(currentQuery);
                }
            }

            internal class PostCommand<TPrevious> : RemoteNavigationSpecification<TResult>
            {
                readonly RemoteNavigationSpecification<TPrevious> _previous;
                readonly Func<TPrevious, BusApi.Remote.AtMostOnce.ICommand<TResult>> _next;
                internal PostCommand(RemoteNavigationSpecification<TPrevious> previous, Func<TPrevious, BusApi.Remote.AtMostOnce.ICommand<TResult>> next)
                {
                    _previous = previous;
                    _next = next;
                }

                public override TResult ExecuteRemoteOn(IUIInteractionApiBrowser busSession)
                {
                    var previousResult = _previous.ExecuteRemoteOn(busSession);
                    var currentCommand = _next(previousResult);
                    return busSession.PostRemote(currentCommand);
                }

                public override async Task<TResult> ExecuteRemoteAsyncOn(IUIInteractionApiBrowser busSession)
                {
                    var previousResult = await _previous.ExecuteRemoteAsyncOn(busSession);
                    var currentCommand = _next(previousResult);
                    return await busSession.PostRemoteAsync(currentCommand);
                }
            }

            internal class PostVoidCommand<TPrevious> : RemoteNavigationSpecification
            {
                readonly RemoteNavigationSpecification<TPrevious> _previous;
                readonly Func<TPrevious, BusApi.Remote.AtMostOnce.ICommand> _next;
                internal PostVoidCommand(RemoteNavigationSpecification<TPrevious> previous, Func<TPrevious, BusApi.Remote.AtMostOnce.ICommand> next)
                {
                    _previous = previous;
                    _next = next;
                }

                public override void ExecuteRemoteOn(IUIInteractionApiBrowser busSession)
                {
                    var previousResult = _previous.ExecuteRemoteOn(busSession);
                    var currentCommand = _next(previousResult);
                    busSession.PostRemote(currentCommand);
                }

                public override async Task ExecuteRemoteAsyncOn(IUIInteractionApiBrowser busSession)
                {
                    var previousResult = await _previous.ExecuteRemoteAsyncOn(busSession);
                    var currentCommand = _next(previousResult);
                    busSession.PostRemote(currentCommand);
                }
            }
        }
    }
}
