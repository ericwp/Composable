﻿using Castle.MicroKernel.Registration;
using Composable.ServiceBus;
using FluentAssertions;
using NServiceBus;
using NUnit.Framework;
using System;

namespace CQRS.Tests.ServiceBus
{
    [TestFixture]
    public class WhenHandlingAMessageThatInheritsOtherMessages : MessageHandlersTestBase
    {
        [SetUp]
        public void RegisterHandlers()
        {
            Container.Register(
                Component.For<CandidateUpdater, IHandleMessages<INamePropertyUpdatedMessage>, IHandleInProcessMessages<IAgePropertyUpdatedMessage>>()
                    .ImplementedBy<CandidateUpdater>().LifestyleScoped(),
                Component.For<CandidateMessageSpy>().Instance(CandidateMessageSpy.Instance).LifestyleSingleton()
                );
        }

        [TearDown]
        public void ResetSpy()
        {
            Container.Resolve<CandidateMessageSpy>().Reset();
        }

        [Test]
        public void When_publishing_a_message_Then_multiple_message_handlers_should_be_invoked()
        {
            //Arrange
            const string name = "Candidate1";
            const int age = 30;
            const string phone = "phoneNumber";
            var editCandidateMessage = new CandidateEditedMessage(name: name, age: age, phone: phone);

            //Act
            SynchronousBus.Publish(editCandidateMessage);

            //Assert
            var candidate = Container.Resolve<CandidateMessageSpy>();
            candidate.Name.Should().Be(name);
            candidate.Age.Should().Be(age);
        }

        [Test]
        public void When_sending_a_message_Then_should_throw_exception()
        {
            //Arrange
            const string name = "Candidate2";
            const int age = 32;
            const string phone = "phoneNumber";
            var editCandidateMessage = new CandidateEditedMessage(name: name, age: age, phone: phone);

            //Act
            Action send = () => SynchronousBus.Send(editCandidateMessage);

            //Assert
            send.ShouldThrow<MultipleMessageHandlersRegisteredException>("multiple handlers registered for message");
        }

        internal interface INamePropertyUpdatedMessage : IMessage
        {
            string Name { get; }
        }

        internal interface IAgePropertyUpdatedMessage : IMessage
        {
            int Age { get; }
        }

        internal interface ICandidateEditedMessage
            : INamePropertyUpdatedMessage,
            IAgePropertyUpdatedMessage
            
        {

        }

        internal class CandidateEditedMessage : ICandidateEditedMessage
        {
            public CandidateEditedMessage(string name,int age,string phone)
            {
                Name = name;
                Age = age;
                Phone = phone;
            }
            public string Name { get; private set; }
            public int Age { get; private set; }
            public string Phone { get; private set; }
        }


        private class CandidateUpdater : 
            IHandleMessages<INamePropertyUpdatedMessage>,
            IHandleInProcessMessages<IAgePropertyUpdatedMessage>
        {
            private readonly CandidateMessageSpy _candidateMessage;

            public CandidateUpdater(CandidateMessageSpy candidateMessage)
            {
                _candidateMessage = candidateMessage;
            }

            public void Handle(INamePropertyUpdatedMessage message)
            {
                _candidateMessage.Name = message.Name;
            }

            public void Handle(IAgePropertyUpdatedMessage message)
            {
                _candidateMessage.Age = message.Age;
            }
        }

        private class CandidateMessageSpy : ISynchronousBusMessageSpy
        {
            public string Name { get; set; }
            public int Age { get; set; }
            public string Phone { get; set; }

            private static CandidateMessageSpy _messageSpy;

            private CandidateMessageSpy()
            {
                
            }
            public static CandidateMessageSpy Instance
            {
                get
                {
                    if(_messageSpy==null) _messageSpy=new CandidateMessageSpy();
                    return _messageSpy;
                }
            }

            public void Reset()
            {
                _messageSpy = new CandidateMessageSpy();
            }
        }
    }
}
