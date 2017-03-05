﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Composable.System.Linq;
using NetMQ;
using NetMQ.Sockets;

namespace NetMqProcess01.__Mine
{
    public static class _01_ReqRouterDealerRep
    {
        const bool UseInProcess = true;
        static string RouterSocket = UseInProcess ? "inproc://router-socket" : "tcp://127.0.0.1:5559";
        static string DealerSocket = UseInProcess ? "inproc://dealer-socket" : "tcp://127.0.0.1:5560";
        static readonly int NumberOfServers = 10;
        static readonly int NumberOfClient = 100;

        public static void Run()
        {
            StartBroker();

            Thread.Sleep(1000);
            StartServers();
            StartClients();
            Console.ReadLine();
        }

        static void StartServers() =>
            1.Through(NumberOfServers)
             .ForEach(serverId =>
                      {
                          new Thread(() =>
                                   {
                                       using(var responseSocket = new ResponseSocket())
                                       {
                                           responseSocket.Connect(DealerSocket);
                                           //Console.WriteLine($"Server: {serverId} started");
                                           while(true)
                                           {
                                               var request = responseSocket.ReceiveMultipartMessage();
                                               var clientId = request.First.ConvertToInt32();

                                               // Console.WriteLine($"Server {serverId} got request from client: {clientId}");
                                               var response = new NetMQMessage(new[]
                                                                               {
                                                                                   new NetMQFrame($"Server:{serverId}, Client:{clientId}")
                                                                               }
                                                                              );

                                               // Console.WriteLine($"Server {serverId} responding to client: {clientId}");
                                               responseSocket.SendMultipartMessage(response);
                                           }
                                       }
                                   }).Start();
                      });

        static long TotalRequests = 0;

        static void StartBroker() =>
            Task.Run(() =>
                     {
                         using(var router = new RouterSocket())
                         using(var dealer = new DealerSocket())
                         {
                             router.Bind(RouterSocket);
                             dealer.Bind(DealerSocket);
                             new Proxy(router, dealer).Start();
                         }
                     });

        static void StartClients() =>
            1.Through(NumberOfClient)
             .ForEach(clientId =>
                      {
                          new Thread(() =>
                                   {
                                       using(var requestSocket = new RequestSocket())
                                       {
                                           requestSocket.Connect(RouterSocket);
                                           //Console.WriteLine($"Client: {clientId} started");
                                           long requests = 0;
                                           while(true)
                                               try
                                               {
                                                   requests++;

                                                   var request = new NetMQMessage();
                                                   request.Append(clientId);

                                                   // Console.WriteLine($"Client:{clientId} sending: {request.First.ConvertToString()}");
                                                   requestSocket.SendMultipartMessage(request);
                                                   var response = requestSocket.ReceiveMultipartMessage()
                                                                               .First.ConvertToString();

                                                   if(!response.Contains($"Client:{clientId}"))
                                                   {
                                                       throw new Exception($"Client:{clientId} got response indended for another client: {response}");
                                                   }

                                                   //if(requests % 1000 == 0)
                                                   //{
                                                   //    Console.WriteLine($"Client:{clientId} has made {requests} requests.");
                                                   //}

                                                   var currentTotalRequests = Interlocked.Increment(ref TotalRequests);
                                                   if(currentTotalRequests % 10000 == 0)
                                                   {
                                                       Console.WriteLine($"Total requests: {currentTotalRequests:N}");
                                                   }

                                                   // Console.WriteLine($"Client:{clientId} got: {response}");
                                               }
                                               catch(Exception e)
                                               {
                                                   Console.WriteLine(e);
                                               }
                                       }
                                   }).Start();
                      });
    }
}
