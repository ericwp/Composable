﻿using System;
using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Linq;
using Castle.Windsor;
using Composable.GenericAbstractions.Time;
using Composable.System;
using Composable.System.Reactive;

namespace Composable.ServiceBus
{
    public class TestingOnlyServiceBus : InProcessServiceBus, IMessageSpy
    {
        readonly DummyTimeSource _timeSource;
        readonly List<ScheduledMessage> _scheduledMessages = new List<ScheduledMessage>(); 
        public TestingOnlyServiceBus(DummyTimeSource timeSource)
        {
            _timeSource = timeSource;
            timeSource.UtcNowChanged.Subscribe(SendDueMessages);
        }

        void SendDueMessages(DateTime currentTime)
        {
            var dueMessages = _scheduledMessages.Where(message => message.SendAt <= currentTime).ToList();
            dueMessages.ForEach(scheduledMessage => Send(scheduledMessage.Message));
            dueMessages.ForEach(message => _scheduledMessages.Remove(message));
        }

        public override void SendAtTime(DateTime sendAt, object message)
        {
            if(_timeSource.UtcNow > sendAt.ToUniversalTime())
            {
                throw new InvalidOperationException("You cannot schedule a message to be sent in the past.");
            }
            _scheduledMessages.Add(new ScheduledMessage(sendAt, message));
        }

        class ScheduledMessage
        {
            public Guid Id { get; }
            public DateTime SendAt { get; }
            public object Message { get; }

            public ScheduledMessage(DateTime sendAt, object message)
            {
                Id = Guid.NewGuid();
                SendAt = sendAt.SafeToUniversalTime();
                Message = message;
            }
        }

        readonly List<IMessage> _dispatchedMessages = new List<IMessage>();
        public IEnumerable<IMessage> DispatchedMessages => _dispatchedMessages;

        protected override void AfterDispatchingMessage(IMessage message)
        {
            _dispatchedMessages.Add(message);
        }
    }    
}