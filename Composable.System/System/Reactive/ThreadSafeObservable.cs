﻿using System;

namespace Composable.System.Reactive
{
    ///<summary>Suplies a simple and thread safe implementation of IObservable.</summary>
    class ThreadSafeObservable<TEvent> : IObservable<TEvent>
    {
        readonly ThreadSafeObserverCollection<TEvent> _observerCollection = new ThreadSafeObserverCollection<TEvent>();

        ///<summary>Invoke <see cref="IObserver{T}.OnNext"/> on each subscribed observer.</summary>
        public void OnNext(TEvent @event)
        {
            _observerCollection.OnNext(@event);
        }

        /// <inheritdoc />
        public IDisposable Subscribe(IObserver<TEvent> observer)
        {
            _observerCollection.Add(observer);
            return new Disposable(() => _observerCollection.Remove(observer));
        }
    }
}