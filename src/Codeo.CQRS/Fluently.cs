using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Codeo.CQRS.Exceptions;
using Dapper;

namespace Codeo.CQRS
{
    public static class Fluently
    {
        public static Configuration Configure()
        {
            return new Configuration();
        }

        public static Configuration Configure(Action<Configuration> builder)
        {
            var configuration = new Configuration();
            builder(configuration);
            return configuration;
        }

        public class Configuration
        {
            internal Configuration()
            {
                WithSnakeCaseMappingEnabled();
            }

            public Configuration WithSnakeCaseMappingEnabled()
            {
                DefaultTypeMap.MatchNamesWithUnderscores = true;
                RemapAllKnownEntities();
                return this;
            }

            public Configuration WithSnakeCaseMappingDisabled()
            {
                DefaultTypeMap.MatchNamesWithUnderscores = false;
                RemapAllKnownEntities();
                return this;
            }

            public Configuration Reset()
            {
                BaseSqlExecutor.RemoveAllExceptionHandlers();
                BaseSqlExecutor.ConnectionFactory = null;
                EntityDoesNotExistException.DebugEnabled = false;
                return this;
            }

            public Configuration WithExceptionHandler<TException>(
                IExceptionHandler<TException?> handler)
                where TException : Exception
            {
                BaseSqlExecutor.InstallExceptionHandler(handler);
                return this;
            }

            public Configuration WithoutExceptionHandler<TException>(
                IExceptionHandler<TException> handler)
                where TException : Exception
            {
                BaseSqlExecutor.UninstallExceptionHandler(handler);
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
            
            public Configuration WithServiceProvider(IServiceProvider serviceProvider)
            {
                BaseSqlExecutor.ServiceProvider = serviceProvider;
                return this;
            }

            public Configuration WithDebugMessagesEnabled()
            {
                EntityDoesNotExistException.DebugEnabled = true;
                return this;
            }

            public Configuration WithDebugMessagesDisabled()
            {
                EntityDoesNotExistException.DebugEnabled = false;
                return this;
            }

            public Configuration WithEntitiesFrom(
                Assembly assembly,
                Func<Type, bool> discriminator)
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

            public static void RemapAllKnownEntities()
            {
                lock (MapLock)
                {
                    var types = BaseSqlExecutor.KnownMappedTypes.Keys.ToArray();
                    BaseSqlExecutor.KnownMappedTypes.Clear();
                    foreach (var type in types)
                    {
                        MapEntityType(type);
                    }
                }
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
                        var columnName = 
                            DefaultTypeMap.MatchNamesWithUnderscores
                            ? column.Replace("_", "")
                            : column;
                        var mappedProperty =
                            type.GetProperties()
                                .FirstOrDefault(
                                    x => x.Name.Equals(
                                        columnName,
                                        StringComparison.OrdinalIgnoreCase
                                    )
                                );

                        return mappedProperty;
                    }
                );
            }
        }
    }
}