﻿using System;
using FluentAssertions;
using NetMQ;
using NetMQ.Sockets;
using NUnit.Framework;

namespace NetMqProcess01.__Mine
{
    [TestFixture]
    public class _001_request_reply_api_exploration
    {
        ResponseSocket _responseSocket;
        RequestSocket _requestSocket;

        [SetUp] public void SetupTask()
        {
            string responseSocketAddress = $"inproc://request-socket-{Guid.NewGuid()}";
            _responseSocket = new ResponseSocket(responseSocketAddress);
            _requestSocket = new RequestSocket(responseSocketAddress);
        }

        [TearDown] public void TearDownTask() {
            _requestSocket.Dispose();
            _requestSocket.Dispose();
        }

        [Test]
        public void Basic_send_from_request_and_receive_at_reply()
        {
            _requestSocket.SendFrame("TEST", more: false);

            var request = _responseSocket.ReceiveFrameString();
            request.Should()
                   .Be("TEST");
        }

        [Test] public void Sending_twice_from_request_throws_anException() {
            _requestSocket.SendFrame("1");
            _requestSocket.Invoking( socket => socket.SendFrame("2"))
                .ShouldThrow<NetMQException>()
                .And.
                ErrorCode.Should().Be(ErrorCode.FiniteStateMachine);
        }
    }
}