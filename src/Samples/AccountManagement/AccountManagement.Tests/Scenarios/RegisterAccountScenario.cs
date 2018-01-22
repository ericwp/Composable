using System;
using AccountManagement.API;
using Composable.Messaging.Buses;

namespace AccountManagement.Tests.Scenarios
{
    class RegisterAccountScenario : ScenarioBase
    {
        readonly IEndpoint _clientEndpoint;

        public Guid AccountId;
        public String Email;
        public string Password;

        public RegisterAccountScenario(IEndpoint clientEndpoint, string email = null, string password = null)
        {
            _clientEndpoint = clientEndpoint;
            AccountId = Guid.NewGuid();
            Password = password ?? TestData.Password.CreateValidPasswordString();
            Email = email ?? TestData.Email.CreateValidEmail().ToString();
        }

        public (AccountResource.Commands.Register.RegistrationAttemptResult Result, AccountResource Account) Execute()
        {
            var result = _clientEndpoint.ExecuteRequest(Api.Command.Register(AccountId, Email, Password));

            switch(result)
            {
                case AccountResource.Commands.Register.RegistrationAttemptResult.Successful:
                    return (result, Api.Query.AccountById(AccountId).ExecuteAsRequestOn(_clientEndpoint));
                case AccountResource.Commands.Register.RegistrationAttemptResult.EmailAlreadyRegistered:
                    return (result, null);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}
