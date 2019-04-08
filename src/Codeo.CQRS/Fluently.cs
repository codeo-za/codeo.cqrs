using System;
using System.Data;
using System.Linq;
using System.Reflection;
using Codeo.CQRS.Exceptions;
using Dapper;

namespace Codeo.CQRS
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
                return WithEntitiesFrom(assembly, x => entityType.IsAssignableFrom(x) && x != entityType);
            }

            public Configuration WithEntitiesFrom(Assembly assembly, Func<Type, bool> discriminator)
            {
                var entityTypes = assembly.GetTypes().Where(discriminator);

                // Setup the date handlers to specify the DateTime Kind to UTC when pulling from the database
                SqlMapper.AddTypeHandler(typeof(DateTime), new DateTimeHandler());
                SqlMapper.AddTypeHandler(typeof(DateTime?), new DateTimeNullableHandler());

                foreach (var eType in entityTypes)
                {
                    MapEntityType(eType);
                }

                return this;
            }

            private static object _mapLock = new object();
            public static void MapEntityType(Type type)
            {
                lock (_mapLock)
                {
                    if (BaseSqlExecutor.KnownMappedTypes.Contains(type))
                    {
                        // may have been added between the start of this call and now
                        return;
                    }
                    SqlMapper.SetTypeMap(type, Map(type));
                    BaseSqlExecutor.KnownMappedTypes.Add(type);
                }
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
                                .FirstOrDefault(
                                    x => x.Name.Equals(cleanedColumnName, StringComparison.OrdinalIgnoreCase));

                        return mappedProperty;
                    }
                );
            }
        }
    }
}