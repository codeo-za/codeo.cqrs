using System;
using Codeo.CQRS.Exceptions;

namespace Codeo.CQRS
{
    public interface IExceptionHandler<in T> where T: Exception
    {
        void Handle(Operation operation, T exception);
    }
}