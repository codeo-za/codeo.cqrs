using System;
using System.Collections.Concurrent;
using System.Transactions;

namespace Codeo.CQRS
{
    public static class TransactionScopeExtensions
    {
        public static TransactionScopeDecorator ProtectAgainstDisposalExceptionsWith(
            this TransactionScope scope,
            ConcurrentDictionary<Guid, Func<Exception, DisposalExceptionHandlerResults>> disposalExceptionHandlers)
        {
            return new TransactionScopeDecorator(scope, disposalExceptionHandlers);
        }
    }
}