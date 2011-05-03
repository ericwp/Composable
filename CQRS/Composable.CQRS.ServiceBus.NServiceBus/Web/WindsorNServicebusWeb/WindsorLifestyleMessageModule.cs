﻿using System;
using System.Collections.Generic;
using NServiceBus;

namespace Composable.CQRS.ServiceBus.NServiceBus.Web.WindsorNServicebusWeb
{
    public class WindsorLifestyleMessageModule : IMessageModule
    {
        [ThreadStatic] private static IDictionary<PerNserviceBusMessageLifestyleManager, object> perThreadEvict;


        public static void RegisterForEviction(PerNserviceBusMessageLifestyleManager manager, object instance)
        {
            if(perThreadEvict == null)
            {
                perThreadEvict = new Dictionary<PerNserviceBusMessageLifestyleManager, object>();
            }
            perThreadEvict.Add(manager, instance);
        }


        public void HandleBeginMessage()
        {
        }


        public void HandleEndMessage()
        {
            EvictInstancesCreatedDuringMessageHandling();
        }


        public void HandleError()
        {
            EvictInstancesCreatedDuringMessageHandling();
        }


        private static void EvictInstancesCreatedDuringMessageHandling()
        {
            if(perThreadEvict == null)
                return;

            foreach(var itemToEvict in perThreadEvict)
            {
                var manager = itemToEvict.Key;
                manager.Evict(itemToEvict.Value);
            }

            perThreadEvict.Clear();
            perThreadEvict = null;
        }
    }
}