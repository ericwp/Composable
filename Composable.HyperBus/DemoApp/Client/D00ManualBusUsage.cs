﻿using System;
using System.Threading.Tasks;
using Composable.HyperBus.APIDraft;
using Composable.HyperBus.DemoApp.ExposedApi.Resources;

namespace Composable.HyperBus.DemoApp.Client
{
    public class D00ManualBusUsage
    {
        private IHyperBus Bus { get; }

        public async Task DemoDirectBusUsage()
        {
            var startPageSomething = await Bus.GetAsync(DemoApplicationApi.StartResource);
            var startPage = await startPageSomething.GetReturnValue();
            var accountsSomething = await Bus.GetAsync(startPage.Links.Accounts);
            var accounts = await accountsSomething.GetReturnValue();
            var account = await Bus.ExecuteAsync(accounts.Commands.Register(email: "someone@somewhere.com", password: "secret"));
        }
    }
}