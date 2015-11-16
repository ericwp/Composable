using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using Composable.System.Collections.Collections;
using Composable.System.Linq;

namespace Composable.CQRS.EventSourcing.EventRefactoring.Migrations
{
    //Yes this class has quite a bit of code that looks overly complex. Like it could be simplified a lot. 
    //What you are seeing is likely optimizations. Please do not change this class for reasons of readability unless you do thorough performance testing and verify that no performance is lost.
    //The performance of this class is extremely important since it is called at least once for every event that is loaded from the event store when you have any migrations activated. It is called A LOT.
    //This is one of those central classes for which optimization is actually vitally important.
    //Each of the optimizations were done with the help of a profiler and running benchmarks on the tested performance improvements time and time again.  
    internal class EventModifier : IEventModifier
    {
        private readonly Action<IReadOnlyList<AggregateRootEvent>> _eventsAddedCallback;
        private LinkedList<AggregateRootEvent> _events;
        private IReadOnlyList<AggregateRootEvent> _replacementEvents;
        private IReadOnlyList<AggregateRootEvent> _insertedEvents;

        public EventModifier(AggregateRootEvent @event, Action<IReadOnlyList<AggregateRootEvent>> eventsAddedCallback)
        {
            Event = @event;
            _eventsAddedCallback = eventsAddedCallback;
        }

        private EventModifier(LinkedListNode<AggregateRootEvent> currentNode, Action<IReadOnlyList<AggregateRootEvent>> eventsAddedCallback)
        {
            _eventsAddedCallback = eventsAddedCallback;
            CurrentNode = currentNode;
            _events = currentNode.List;
        }

        public AggregateRootEvent Event { get; private set; }

        private LinkedListNode<AggregateRootEvent> _currentNode;
        private LinkedListNode<AggregateRootEvent> CurrentNode
        {
            get
            {
                if (_events == null)
                {
                    _events = new LinkedList<AggregateRootEvent>();
                    _currentNode = _events.AddFirst(Event);
                }
                return _currentNode;
            }
            set
            {
                _currentNode = value;
                Event = _currentNode.Value;
            }
        }

        public void Replace(IReadOnlyList<AggregateRootEvent> events)
        {
            Contract.Assert(_replacementEvents == null, $"You can only call {nameof(Replace)} once");
            Contract.Assert(Event.GetType() != typeof(EventStreamEndedEvent), "You cannot call replace on the event that signifies the end of the stream");

            _replacementEvents = events;

            _replacementEvents.ForEach(
                (e, index) =>
                {
                    e.AggregateRootVersion = Event.AggregateRootVersion + index;
                    e.Replaces = Event.InsertionOrder;
                    e.AggregateRootId = Event.AggregateRootId;
                });

            CurrentNode = CurrentNode.Replace(_replacementEvents).First();
            _eventsAddedCallback.Invoke(_replacementEvents);
        }

        public void InsertBefore(IReadOnlyList<AggregateRootEvent> insert)
        {
            Contract.Assert(_insertedEvents == null, $"You can only call {nameof(InsertBefore)} once");

            _insertedEvents = insert.ToList();

            _insertedEvents.ForEach(
                (e, index) =>
                {
                    e.InsertBefore = Event.InsertionOrder;
                    e.AggregateRootVersion = Event.AggregateRootVersion + index;
                    e.AggregateRootId = Event.AggregateRootId;
                });

            if (Event.GetType() == typeof(EventStreamEndedEvent))
            {
                _insertedEvents.ForEach(@event => @event.InsertBefore = null);//We are at the end of the stream. Claiming to insert before it makes no sense
            }

            CurrentNode.ValuesFrom().ForEach((@event, index) => @event.AggregateRootVersion += _insertedEvents.Count);

            CurrentNode.AddBefore(_insertedEvents);
            _eventsAddedCallback.Invoke(_insertedEvents);
        }

        internal IReadOnlyList<AggregateRootEvent> MutatedHistory => _events != null ? _events.ToList() : new List<AggregateRootEvent> { Event };

        //Yes, doing this optimization her does make a very significant performance improvement proven throught actual profiling and benchmarking. Do NOT replace this code with a linq expression!
        public IEnumerable<EventModifier> GetHistory()
        {
            if (_events == null)
            {
                yield return this;
                yield break;
            }

            var node = _events.First;
            while (node != null)
            {
                yield return new EventModifier(node, _eventsAddedCallback);
                node = node.Next;
            }

        }
    }
}