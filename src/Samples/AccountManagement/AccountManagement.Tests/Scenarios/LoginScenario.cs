﻿using System.Threading.Tasks;
using AccountManagement.API;
using Composable.Messaging.Buses;
using Composable.System.Linq;
using Composable.System.Threading;

namespace AccountManagement.Tests.Scenarios
{
    class LoginScenario
    {
        readonly IServiceBus _bus;
        public string Password { get; set; }
        public string Email { get; set; }


        public static LoginScenario Create(IServiceBus bus)
        {
            var registerAccountScenario = new RegisterAccountScenario(bus);
            registerAccountScenario.Execute();
            return new LoginScenario(bus, registerAccountScenario.Email, registerAccountScenario.Password);
        }

        public LoginScenario(IServiceBus bus, AccountResource account, string password):this(bus, account.Email.ToString(), password)
        {}

        public LoginScenario(IServiceBus bus, string email, string password)
        {
            Email = email;
            Password = password;
            _bus = bus;
        }

        public LoginAttemptResult Execute()
        {
            return _bus.Get(AccountApi.Start)
                .Post(start => start.Commands.Login.Mutate(@this =>
                       {
                           @this.Email = Email;
                           @this.Password = Password;
                       }))
                .Execute();
        }
    }
}
