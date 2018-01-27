﻿using Composable.Messaging;
using Composable.Messaging.Buses;

namespace AccountManagement.API
{
    public class StartResource
    {
        public Command Commands { get; private set; } = new Command();

        public Query Queries { get; private set; } = new Query();

        public class Query
        {
            public BusApi.Remote.Query.RemoteEntityResourceQuery<AccountResource> AccountById { get; private set; } = new BusApi.Remote.Query.RemoteEntityResourceQuery<AccountResource>();
        }

        public class Command
        {
            public AccountResource.Command.LogIn Login { get; private set; } = new AccountResource.Command.LogIn();
            public AccountResource.Command.Register Register { get; private set; } = new AccountResource.Command.Register();
        }
    }
}
