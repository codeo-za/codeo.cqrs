using System;
using Codeo.CQRS.Exceptions;

namespace Codeo.CQRS
{
    public interface IExceptionHandler
    {
    }

    public enum ExceptionHandlingStrategy
    {
        Throw,
        Suppress
    }

    public interface IExceptionHandler<in T>: IExceptionHandler where T: Exception
    {
        ExceptionHandlingStrategy Handle(Operation operation, T exception);
    }
}