using System;
using System.Collections.Concurrent;
using System.Transactions;

namespace Codeo.CQRS
{
    public enum DisposalExceptionHandlerResults
    {
        Handled,
        Unhandled
    }
    
    public class TransactionScopeDecorator : IDisposable
    {
        private readonly TransactionScope _scope;

        private readonly ConcurrentDictionary<Guid, Func<Exception, DisposalExceptionHandlerResults>>
            _disposalExceptionHandlers;

        internal TransactionScopeDecorator(
            TransactionScope scope,
            ConcurrentDictionary<Guid, Func<Exception, DisposalExceptionHandlerResults>> disposalExceptionHandlers)
        {
            _scope = scope;
            _disposalExceptionHandlers = disposalExceptionHandlers;
        }

        public void Complete()
        {
            _scope.Complete();
        }

        public void Dispose()
        {
            try
            {
                _scope.Dispose();
            }
            catch (Exception ex)
            {
                foreach (var handler in _disposalExceptionHandlers)
                {
                    if (handler.Value(ex) == DisposalExceptionHandlerResults.Handled)
                    {
                        return;
                    }
                }

                throw;
            }
        }
    }
}