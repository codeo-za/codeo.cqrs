using System;
using System.Linq;
using System.Reflection;
using Codeo.CQRS.Exceptions;
using Dapper;

namespace Codeo.CQRS
{
    /// <summary>
    /// Fluent configuration for Codeo.CQRS
    /// </summary>
    public static class Fluently
    {
        /// <summary>
        /// Starts a new configuration chain.
        /// </summary>
        /// <returns></returns>
        public static Configuration Configure()
        {
            return new Configuration();
        }

        /// <summary>
        /// Provides configuration for Codeo.CQRS
        /// </summary>
        public class Configuration
        {
            internal Configuration()
            {
                WithSnakeCaseMappingEnabled();
            }

            /// <summary>
            /// Enables snake_case mapping (default)
            /// </summary>
            /// <returns></returns>
            public Configuration WithSnakeCaseMappingEnabled()
            {
                DefaultTypeMap.MatchNamesWithUnderscores = true;
                RemapAllKnownEntities();
                return this;
            }

            /// <summary>
            /// Disables snake_case mapping
            /// </summary>
            /// <returns></returns>
            public Configuration WithSnakeCaseMappingDisabled()
            {
                DefaultTypeMap.MatchNamesWithUnderscores = false;
                RemapAllKnownEntities();
                return this;
            }

            /// <summary>
            /// Resets all existing Codeo.CQRS configuration
            /// </summary>
            /// <returns></returns>
            public Configuration Reset()
            {
                BaseSqlExecutor.RemoveAllExceptionHandlers();
                BaseSqlExecutor.ConnectionFactory = null;
                EntityDoesNotExistException.DebugEnabled = false;
                return this;
            }

            /// <summary>
            /// Adds an exception handler to the available collection
            /// for exceptions of type TException
            /// </summary>
            /// <param name="handler"></param>
            /// <typeparam name="TException"></typeparam>
            /// <returns></returns>
            public Configuration WithExceptionHandler<TException>(
                IExceptionHandler<TException> handler)
                where TException : Exception
            {
                BaseSqlExecutor.InstallExceptionHandler(handler);
                return this;
            }

            /// <summary>
            /// Removes an existing exception handler, if it is found
            /// </summary>
            /// <param name="handler"></param>
            /// <typeparam name="TException"></typeparam>
            /// <returns></returns>
            public Configuration WithoutExceptionHandler<TException>(
                IExceptionHandler<TException> handler)
                where TException : Exception
            {
                BaseSqlExecutor.UninstallExceptionHandler(handler);
                return this;
            }

            /// <summary>
            /// Register entities from the provided assembly with the Dapper mapper
            /// </summary>
            /// <param name="assembly"></param>
            /// <returns></returns>
            public Configuration WithEntitiesFrom(Assembly assembly)
            {
                var entityType = typeof(IEntity);
                return WithEntitiesFrom(
                    assembly,
                    x => entityType.IsAssignableFrom(x) && x != entityType
                );
            }

            /// <summary>
            /// Provide a connection factory for all subsequent
            /// commands / queries
            /// </summary>
            /// <param name="connectionFactory"></param>
            /// <returns></returns>
            public Configuration WithConnectionFactory(
                IDbConnectionFactory connectionFactory
            )
            {
                BaseSqlExecutor.ConnectionFactory = connectionFactory;
                return this;
            }

            /// <summary>
            /// Enable more detailed diagnostic error messages
            /// </summary>
            /// <returns></returns>
            public Configuration WithDebugMessagesEnabled()
            {
                EntityDoesNotExistException.DebugEnabled = true;
                return this;
            }

            /// <summary>
            /// Enable simpler
            /// </summary>
            /// <returns></returns>
            public Configuration WithDebugMessagesDisabled()
            {
                EntityDoesNotExistException.DebugEnabled = false;
                return this;
            }

            /// <summary>
            /// Register entities from the provided assembly where the types
            /// match a provided discriminator
            /// </summary>
            /// <param name="assembly"></param>
            /// <param name="discriminator"></param>
            /// <returns></returns>
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

            /// <summary>
            /// Remaps all known entity types - required when switching
            /// mappings between with snake_case and without snake_case
            /// </summary>
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

            /// <summary>
            /// Adds a single type to be mapped. If the type is already known,
            /// the existing mapping is left intact.
            /// </summary>
            /// <param name="type"></param>
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