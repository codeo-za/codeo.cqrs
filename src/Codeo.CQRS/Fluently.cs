using System;
using System.Linq;
using System.Reflection;
using Dapper;

namespace Codeo.CQRS
{
    public static class Fluently
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

            public Configuration WithExceptionHandler<TException>(
                IExceptionHandler<TException> handler) 
                where TException: Exception
            {
                BaseSqlExecutor.AddExceptionHandler(handler);
                return this;
            }

            public Configuration WithEntitiesFrom(Assembly assembly)
            {
                var entityType = typeof(IEntity);
                return WithEntitiesFrom(
                    assembly, 
                    x => entityType.IsAssignableFrom(x) && x != entityType);
            }

            public Configuration WithConnectionFactory(IDbConnectionFactory connectionFactory)
            {
                BaseSqlExecutor.ConnectionFactory = connectionFactory;
                return this;
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

            private static readonly object MapLock = new object();

            public static void MapEntityType(Type type)
            {
                lock (MapLock)
                {
                    if (BaseSqlExecutor.KnownMappedTypes.ContainsKey(type))
                    {
                        // may have been added between the start of this call and now
                        return;
                    }
                    SqlMapper.SetTypeMap(type, Map(type));
                    BaseSqlExecutor.KnownMappedTypes.TryAdd(type, true);
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