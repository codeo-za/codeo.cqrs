using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using Codeo.CQRS.Exceptions;
using Dapper;

namespace Codeo.CQRS
{
    internal static class TypeExtensions
    {
        /// <summary>
        /// Attempts to get the item type of a colleciton
        /// </summary>
        /// <param name="collectionType">Type to inspect</param>
        /// <returns>Item type, if it can be found, or null</returns>
        internal static Type GetCollectionItemType(this Type collectionType)
        {
            if (collectionType.IsArray)
                return collectionType.GetElementType();
            if (collectionType.IsGenericType())
                return collectionType.GenericTypeArguments[0];
            var enumerableInterface = collectionType.TryGetEnumerableInterface();
            if (enumerableInterface != null)
                return enumerableInterface.GenericTypeArguments[0];
            return null;
        }
        /// <summary>
        /// Attempts to get the implemented Generic IEnumerable interface for a type, if possible
        /// </summary>
        /// <param name="srcType">Type to search for the interface</param>
        /// <returns>Generic IEnumerable type implemented if found or null otherwise</returns>
        internal static Type TryGetEnumerableInterface(this Type srcType)
        {
            return srcType.IsGenericOfIEnumerable()
                ? srcType
                : srcType.GetInterfaces().FirstOrDefault(IsGenericOfIEnumerable);
        }
        
        internal static bool IsGenericOfIEnumerable(this Type arg)
        {
            return arg.IsGenericOf(typeof(IEnumerable<>));
        }
        /// <summary>
        /// Tests if a type is a generic of a given generic type (eg typeof(List&lt;&gt;))
        /// </summary>
        /// <param name="t">type to operate on</param>
        /// <param name="genericTest">type to test against (eg typeof(List&lt;&gt;))</param>
        /// <returns>True if the input type is a match, false otherwise</returns>
        internal static bool IsGenericOf(this Type t, Type genericTest)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == genericTest;
        }
        /// <summary>
        /// Provides an extension method mimicking the full framework
        /// IsGenericType for a single point of code usage
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        internal static bool IsGenericType(this Type t)
        {
            return t
#if NETSTANDARD
                   .GetTypeInfo()
#endif
                   .IsGenericType;
        }
    }

    public abstract class BaseSqlExecutor
    {
        internal static Func<IDbConnection> ConnectionFactory = () => throw new ConfigurationException("ConnectionFactory is not defined");
        internal static Dictionary<Type, Action<Operation, Exception>> ExceptionHandlers = new Dictionary<Type, Action<Operation, Exception>>();
        public ICache Cache { get; set; } = new NoCache();

        internal static readonly HashSet<Type> KnownMappedTypes = new HashSet<Type>();

        public IEnumerable<T> SelectMany<T>(string sql, object parameters = null)
        {
            return QueryCollection<T>(Operation.Select, sql, parameters)
                   ??
                   // there are many usages of Query<T> where the result
                   //    isn't checked, but is immediately chained into LINQ,
                   //    which simply fails with a NullReferenceException. Better
                   //    to catch it here.
                   throw new InvalidOperationException(
                       $"{GetType()}: QueryExecutor<T> where T is IEnumerable<> should return empty collection rather than null."
                   );
        }

        public T SelectFirst<T>(string sql, object parameters = null)
        {
            return QueryFirst<T>(Operation.Select, sql, parameters);
        }


        /// <summary>
        /// Selects multiple results from a horizontally joined query result. (2 Types)
        /// </summary>
        /// <typeparam name="TFirst">The type of the first result object.</typeparam>
        /// <typeparam name="TSecond">The type of the second result object.</typeparam>
        /// <typeparam name="TReturn">The type of the returned result object.</typeparam>
        /// <param name="sql">The SQL query</param>
        /// <param name="function">The function on how to handle the returned results</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public List<TReturn> SelectMulti<TFirst, TSecond, TReturn>(
            string sql,
            Func<TFirst, TSecond, TReturn> function,
            object parameters = null)
        {
            EnsureDapperKnowsAbout<TFirst>();
            EnsureDapperKnowsAbout<TSecond>();
            using (var connection = CreateOpenConnection())
            {
                List<TReturn> Execute(IDbConnection conn)
                {
                    return conn.Query(sql, function, parameters).ToList();
                }

                return SelectRowsOnConnection(connection, Execute);
            }
        }

        private IDbConnection CreateOpenConnection()
        {
            var result = ConnectionFactory();
            if (result.State != ConnectionState.Open)
            {
                result.Open();
            }
            return result;
        }

        /// <summary>
        /// Selects multiple results from a horizontally joined query result. (3 Types)
        /// </summary>
        /// <typeparam name="TFirst">The type of the first result object.</typeparam>
        /// <typeparam name="TSecond">The type of the second result object.</typeparam>
        /// <typeparam name="TThird">The type of the third result object.</typeparam>
        /// <typeparam name="TReturn">The type of the returned result object.</typeparam>
        /// <param name="sql">The SQL query</param>
        /// <param name="function">The function on how to handle the returned results</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns></returns>
        public IEnumerable<TReturn> SelectMulti<TFirst, TSecond, TThird, TReturn>(
            string sql,
            Func<TFirst, TSecond, TThird, TReturn> function,
            object parameters = null
        )
        {
            EnsureDapperKnowsAbout<TFirst>();
            EnsureDapperKnowsAbout<TSecond>();
            EnsureDapperKnowsAbout<TThird>();
            using (var connection = CreateOpenConnection())
            {
                List<TReturn> Execute(IDbConnection conn)
                {
                    return conn.Query(sql, function, parameters).ToList();
                }

                return SelectRowsOnConnection(
                    connection,
                    Execute
                );
            }
        }

        private List<TReturnItem> SelectRowsOnConnection<TReturnItem>(
            IDbConnection connection,
            Func<IDbConnection, List<TReturnItem>> executor
        )
        {
            try
            {
                return executor(connection);
            }
            catch (Exception ex)
            {
                if (!TryHandleException(Operation.Select, ex))
                {
                    throw;
                }

                return default(List<TReturnItem>);
            }
        }

        private Action<Operation, Exception> FindHandlerFor(Exception ex)
        {
            if (ExceptionHandlers.TryGetValue(ex.GetType(), out var handler))
            {
                return handler;
            }
            // TODO: try to find derived handler? probably travel all the way up to Exception -- would be useful
            // for the caller to be able to install a generic Exception handler
            return null;
        }

        private bool TryHandleException(Operation operation, Exception ex)
        {
            var handler = FindHandlerFor(ex);
            if (handler == null)
            {
                return false;
            }
            handler.Invoke(operation, ex);
            return true;
        }


        /// <summary>
        /// Selects multiple results from a vertically joined query result. (multiple select statements)
        /// </summary>
        /// <param name="sql">The SQL query.</param>
        /// <param name="parameters">The parameters.</param>
        /// <param name="callback">The callback action on how to handle the results.</param>
        public void SelectMulti(string sql, object parameters, Action<SqlMapper.GridReader> callback)
        {
            using (var connection = CreateOpenConnection())
            {
                SelectMultiOnConnection(
                    connection,
                    sql,
                    parameters,
                    callback
                );
            }
        }

        private void SelectMultiOnConnection(
            IDbConnection connection,
            string sql,
            object parameters,
            Action<SqlMapper.GridReader> callback
        )
        {
            try
            {
                callback(
                    connection.QueryMultiple(sql, parameters)
                );
            }
            catch (Exception ex)
            {
                FindHandlerFor(ex)?.Invoke(Operation.Select, ex);
            }
        }

        public IEnumerable<T> UpdateGetList<T>(string sql, object parameters = null)
        {
            return QueryCollection<T>(Operation.Update, sql, parameters);
        }

        public T UpdateGetFirst<T>(string sql, object parameters = null)
        {
            return QueryFirst<T>(Operation.Update, sql, parameters);
        }

        public IEnumerable<T> InsertGetList<T>(string sql, object parameters = null)
        {
            return QueryCollection<T>(Operation.Insert, sql, parameters);
        }

        public T InsertGetFirst<T>(string sql, object parameters = null)
        {
            return QueryFirst<T>(Operation.Insert, sql, parameters);
        }

        public IEnumerable<T> DeleteGetList<T>(string sql, object parameters = null)
        {
            return QueryCollection<T>(Operation.Delete, sql, parameters);
        }

        public T DeleteGetFirst<T>(string sql, object parameters = null)
        {
            return QueryFirst<T>(Operation.Delete, sql, parameters);
        }


        public int ExecuteUpdate(string sql, object parameters = null)
        {
            return Execute(Operation.Update, sql, parameters);
        }

        public int ExecuteInsert(string sql, object parameters = null)
        {
            return Execute(Operation.Insert, sql, parameters);
        }

        public int ExecuteDelete(string sql, object parameters = null)
        {
            return Execute(Operation.Delete, sql, parameters);
        }

        /// <summary>
        /// Executes a query returning a list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="operation"></param>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private IEnumerable<T> QueryCollection<T>(Operation operation, string sql, object parameters)
        {
            EnsureDapperKnowsAbout<T>();
            using (var connection = CreateOpenConnection())
            {
                return RunListResultOnConnection<T>(
                    operation,
                    connection,
                    sql,
                    parameters
                );
            }
        }

        private IEnumerable<T> RunListResultOnConnection<T>(
            Operation operation,
            IDbConnection connection,
            string sql,
            object parameters
        )
        {
            try
            {
                return connection.Query<T>(sql, parameters);
            }
            catch (Exception ex)
            {
                if (!TryHandleException(operation, ex))
                {
                    throw;
                }

                return new List<T>();
            }
        }

        /// <summary>
        /// Executes a query returning a list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="operation"></param>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private T QueryFirst<T>(Operation operation, string sql, object parameters)
        {
            EnsureDapperKnowsAbout<T>();
            using (var connection = CreateOpenConnection())
            {
                return RunSingleResultQueryOnConnection(
                    operation,
                    connection,
                    conn => conn.QueryFirst<T>(sql, parameters));
            }
        }

        private T RunSingleResultQueryOnConnection<T>(
            Operation operation,
            IDbConnection connection,
            Func<IDbConnection, T> queryExecutor)
        {
            try
            {
                return queryExecutor(connection);
            }
            catch (Exception ex)
            {
                if (!TryHandleException(operation, ex))
                {
                    throw;
                }

                return default(T);
            }
        }


        /// <summary>
        /// Executes a query returning a list
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="operation"></param>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private int Execute(Operation operation, string sql, object parameters)
        {
            using (var connection = CreateOpenConnection())
            {
                return ExecuteOnConnection(connection, operation, sql, parameters);
            }
        }

        private int ExecuteOnConnection(
            IDbConnection connection,
            Operation operation,
            string sql,
            object parameters
        )
        {
            try
            {
                return connection.Execute(sql, parameters);
            }
            catch (Exception ex)
            {
                if (!TryHandleException(operation, ex))
                {
                    throw;
                }

                return default(int);
            }
        }

        private void EnsureDapperKnowsAbout<T>()
        {
            var type = typeof(T);
            type = type.GetCollectionItemType() ?? type;
            if (KnownMappedTypes.Contains(type))
            {
                return;
            }
            Fluently.Configuration.MapEntityType(type);
        }
    }

    public class ConfigurationException : Exception
    {
        public ConfigurationException(string message): base(message)
        {
            throw new NotImplementedException();
        }
    }

    public class UnhandledException : Exception
    {
        public UnhandledException(Exception original) : base("Unhandled exception", original)
        {
        }
    }
}