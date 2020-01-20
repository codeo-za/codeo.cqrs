using System;
using Codeo.CQRS.Exceptions;

namespace Codeo.CQRS
{
    public interface IExceptionHandler
    {
    }

    public interface IExceptionHandler<in T>: IExceptionHandler where T: Exception
    {
        bool Handle(Operation operation, T exception);
    }
}