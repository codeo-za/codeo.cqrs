using System;
using System.Data;
using System.Linq;
using System.Reflection;
using Codeo.CQRS.MySql.Exceptions;
using Dapper;

namespace Codeo.CQRS.MySql
{
    public class Fluently
    {
        public static Configuration Configure()
        {
            return new Configuration();
        }

        public class Configuration
        {
            internal Configuration()
            {
            }

            public Configuration WithConnectionProvider(Func<IDbConnection> factory)
            {
                BaseSqlExecutor.ConnectionFactory = factory;
                return this;
            }

            public Configuration WithExceptionHandler<TException>(Action<Operation, Exception> handler)
                where TException : Exception
            {
                BaseSqlExecutor.ExceptionHandlers[typeof(TException)] = handler;
                return this;
            }

            public Configuration WithCache(ICache cache)
            {
                QueryExecutor.Cache = cache;
                CommandExecutor.Cache = cache;
                return this;
            }

            public Configuration WithQueryExecutorFactory(Func<IQueryExecutor> factory)
            {
                CommandExecutor.QueryExecutorFactory = factory;
                return this;
            }

            public Configuration WithEntitiesFrom(Assembly assembly)
            {
                var entityType = typeof(IEntity);
                var entityTypes = assembly.GetTypes().Where(x => entityType.IsAssignableFrom(x) && x != entityType);

                // Setup the date handlers to specify the DateTime Kind to UTC when pulling from the database
                SqlMapper.AddTypeHandler(typeof(DateTime), new DateTimeHandler());
                SqlMapper.AddTypeHandler(typeof(DateTime?), new DateTimeNullableHandler());

                foreach (var eType in entityTypes)
                {
                    SqlMapper.SetTypeMap(eType, Map(eType));
                }
                return this;
            }
            
            private static CustomPropertyTypeMap Map(Type eType)
            {
                return new CustomPropertyTypeMap(
                    eType,
                    (type, column) =>
                    {
                        var cleanedColumnName = column.Replace("_", "");
                        var mappedProperty =
                            type.GetProperties()
                                .FirstOrDefault(x => x.Name.Equals(cleanedColumnName, StringComparison.OrdinalIgnoreCase));

                        return mappedProperty;
                    }
                );
            }
        }
    }
}