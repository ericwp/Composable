﻿using System.Collections.Generic;
using Composable.CQRS.CQRS.Query.Models.Generators;
using Composable.SystemExtensions.Threading;
using JetBrains.Annotations;

namespace AccountManagement.UI.QueryModels.EventStoreGenerated
{
    [UsedImplicitly]
    class AccountManagementEventStoreQueryModelReader : QueryModelGeneratingDocumentDbReader
    {
        public AccountManagementEventStoreQueryModelReader(
            ISingleContextUseGuard usageGuard,
            IEnumerable<IAccountManagementQueryModelGenerator> documentGenerators)
            : base(usageGuard, documentGenerators) {}
    }
}
