﻿using System;
using System.Transactions;

namespace Composable.SystemExtensions.TransactionsCE
{
    static class TransactionCE
    {
        internal static void OnCommit(this Transaction @this, Action action)
        {
            @this.TransactionCompleted += (sender, args) =>
            {
                if(args.Transaction.TransactionInformation.Status == TransactionStatus.Committed)
                {
                    action();
                }
            };
        }

        internal static void OnAbort(this Transaction @this, Action action)
        {
            @this.TransactionCompleted += (sender, args) =>
            {
                if(args.Transaction.TransactionInformation.Status == TransactionStatus.Aborted)
                {
                    action();
                }
            };
        }
    }
}
