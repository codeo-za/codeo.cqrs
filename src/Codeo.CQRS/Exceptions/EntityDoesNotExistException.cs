using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Codeo.CQRS.Exceptions
{
    /// <summary>
    /// Thrown when the caller expects an entity to exist for
    /// the provided predicates, but it doesn't
    /// </summary>
    public class EntityDoesNotExistException : Exception
    {
        /// <summary>
        /// Set to true if you'd like detailed error messages
        /// </summary>
        public static bool DebugEnabled = false;

        /// <summary>
        /// Name of the type of entity (eg "customer")
        /// </summary>
        public string EntityName { get; }

        /// <summary>
        /// What predicates, if any, were used to attempt to
        /// retrieve the record
        /// </summary>
        public object Predicates { get; }

        /// <inheritdoc />
        public EntityDoesNotExistException(string sql)
            : this(sql, null, null)
        {
        }

        /// <inheritdoc />
        public EntityDoesNotExistException(
            string entityNameOrSql,
            object predicates
        ) : this(entityNameOrSql, predicates, null)
        {
        }

        /// <inheritdoc />
        public EntityDoesNotExistException(
            string entityNameOrSql,
            object predicates,
            Exception innerException
        ) : base(CreateMessageFor(entityNameOrSql, predicates), innerException)
        {
            EntityName = entityNameOrSql;
            Predicates = predicates;
        }

        private static string CreateMessageFor(
            string entityNameOrSql,
            object predicates
        )
        {
            return DebugEnabled
                ? CreateDiagnosticMessageFor(entityNameOrSql, predicates)
                : CreateSimpleMessageFor(entityNameOrSql);
        }

        private static string CreateSimpleMessageFor(string entityNameOrSql)
        {
            return LooksLikeSql(entityNameOrSql)
                ? "No matching records for query"
                : $"{entityNameOrSql} record does not exist for predicate";
        }

        private static string CreateDiagnosticMessageFor(string entityNameOrSql, object predicates)
        {
            return LooksLikeSql(entityNameOrSql)
                ? $"No records found for query:\n${entityNameOrSql}\nwith predicate:\n{Dump(predicates)}"
                : $"{entityNameOrSql} record does not exist for predicate:\n{Dump(predicates)}";
        }


        private static bool LooksLikeSql(string str)
        {
            return WhiteSpaceRegex.Matches(str)
                .Cast<Match>()
                .Any(w => SqlKeyWords.Contains(w.Value));
        }

        private static readonly Regex WhiteSpaceRegex = new Regex("[^\\s]");

        private static readonly HashSet<string> SqlKeyWords = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase
        )
        {
            "insert",
            "update",
            "delete",
            "select"
        };

        private static string Dump(object predicates)
        {
            if (predicates is string str)
            {
                return str;
            }

            return JsonConvert.SerializeObject(
                predicates,
                PredicateSerializerSettings
            );
        }


        private static readonly JsonSerializerSettings PredicateSerializerSettings =
            new JsonSerializerSettings()
            {
                Formatting = Formatting.Indented,
                PreserveReferencesHandling = PreserveReferencesHandling.All
            };
    }
}