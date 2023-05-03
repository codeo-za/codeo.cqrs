using System;
using Codeo.CQRS.Exceptions;

namespace Codeo.CQRS
{
    /// <summary>
    /// Base ExceptionHandler interface, used to hold handlers in a collection
    /// </summary>
    public interface IExceptionHandler
    {
    }

    /// <summary>
    /// Strategy to follow after an exception handler has run: choose
    /// to rethrow the exception or to suppress it. When an exception
    /// is suppressed, if there is any return value expected, the default
    /// value for that type will be returned.
    /// </summary>
    public enum ExceptionHandlingStrategy
    {
        /// <summary>
        /// Re-throw the exception
        /// </summary>
        Throw,
        /// <summary>
        /// Suppress the exception and return
        /// the default value for the operation
        /// </summary>
        Suppress
    }

    /// <summary>
    /// Contract for an exception handler to install as one of the default
    /// exception handlers during command / query execution
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public interface IExceptionHandler<in T>: IExceptionHandler where T: Exception
    {
        /// <summary>
        /// Choose what to do with the exception: rethrow it or suppress and return
        /// the default value, if any, for the operation
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="exception"></param>
        /// <returns></returns>
        ExceptionHandlingStrategy Handle(Operation operation, T exception);
    }
}