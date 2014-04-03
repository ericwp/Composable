﻿using System.Collections.Generic;
using Composable.CQRS.Query.Models.Generators;
using Composable.KeyValueStorage;
using Composable.SystemExtensions.Threading;

namespace AccountManagement.UI.QueryModels.Generators
{
    internal class AccountManagementQueryModelGeneratingDocumentDbReader : QueryModelGeneratingDocumentDbReader
    {
        public AccountManagementQueryModelGeneratingDocumentDbReader(
            ISingleContextUseGuard usageGuard, 
            IDocumentDbSessionInterceptor interceptor,
            IEnumerable<IAccountManagementQueryModelGenerator> documentGenerators) 
            : base(usageGuard, interceptor, documentGenerators)
        {}    
    }
}