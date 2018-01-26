﻿// ReSharper disable MemberCanBeMadeStatic.Global we want the fluid navigation to be composable with other APIs (AccountApi as a member property in a composite API for a composite UI etc) so static navigation is out.
// ReSharper disable MemberCanBeMadeStatic.Local
using System;
using Composable.Messaging;
using Composable.Messaging.Buses;


namespace AccountManagement.API
{
    /// <summary>
    /// This class provides the ability to use type safe API navigation from a type that does not run on .Net. For instance via Typescript in browser.
    /// We generate typescript interfaces for each of the resources exposed via the Queries and commands ultimately reachable through the Start Query.
    /// A generic browser type can the be used to navigate the whole API remotely.
    /// For .Net clients the next class in this file is a far more convenient way to consume the API.
    /// </summary>
    public static class AccountWebClientApi
    {
        public static readonly SelfGeneratingResourceQuery<StartResource> Start = SelfGeneratingResourceQuery<StartResource>.Instance;
    }


    /// <summary>
    /// This is the entry point to the API for all .Net clients. It provides a simple intuitive fluent API for accessing all of the functionality in the AccountManagement application.
    /// </summary>
    public class AccountApi
    {
        public static AccountApi Instance => new AccountApi();

        RemoteNavigationSpecification<StartResource> Start => RemoteNavigationSpecification.GetRemote(AccountWebClientApi.Start);

        public QuerySection Query => new QuerySection();
        public CommandsSection Command => new CommandsSection();

        public class QuerySection
        {
            static readonly RemoteNavigationSpecification<StartResource.Query> Queries = Instance.Start.Select(start => start.Queries);

            public RemoteNavigationSpecification<AccountResource> AccountById(Guid accountId) => Queries.GetRemote(queries => queries.AccountById.WithId(accountId));
        }

        public class CommandsSection
        {
            static RemoteNavigationSpecification<StartResource.Command> Commands => Instance.Start.Select(start => start.Commands);

            public RemoteNavigationSpecification<AccountResource.Command.Register> Register() => Commands.Select(commands => commands.Register);
            public RemoteNavigationSpecification<AccountResource.Command.Register.RegistrationAttemptResult> Register(Guid accountId, string email, string password) => Commands.PostRemote(commands =>  commands.Register.WithValues(accountId, email, password));

            public RemoteNavigationSpecification<AccountResource.Command.LogIn> Login() => Commands.Select(commands => commands.Login);
            public RemoteNavigationSpecification<AccountResource.Command.LogIn.LoginAttemptResult> Login(string email, string password) => Commands.PostRemote(commands => commands.Login.WithValues(email, password));
        }
    }
}
