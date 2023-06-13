using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

// ReSharper disable UnusedAutoPropertyAccessor.Global
// ReSharper disable MemberCanBePrivate.Global

namespace Codeo.CQRS.Exceptions
{
    [Serializable]
    public class EntityDoesNotExistException : Exception
    {
        /// <summary>
        /// Set to true if you'd like detailed error messages
        /// </summary>
        public static bool DebugEnabled = false;
        public string? EntityName { get; }
        public object? Predicates { get; }
        
        protected EntityDoesNotExistException(SerializationInfo info, StreamingContext streamingContext) : base(info,
            streamingContext)
        {
            // for serializable logging support.  
        }

        public EntityDoesNotExistException(
            string? entityNameOrSql,
            object? predicates
        ) : this(entityNameOrSql, predicates, null)
        {
        }

        public EntityDoesNotExistException(
            string? entityNameOrSql,
            object? predicates = null,
            Exception? innerException = null) : base(CreateMessageFor(entityNameOrSql, predicates), innerException)
        {
            EntityName = entityNameOrSql;
            Predicates = predicates;
        }

        private static string CreateMessageFor(
            string? entityNameOrSql,
            object? predicates
        )
        {
            return DebugEnabled
                ? CreateDiagnosticMessageFor(entityNameOrSql, predicates)
                : CreateSimpleMessageFor(entityNameOrSql);
        }

        private static string CreateSimpleMessageFor(string? entityNameOrSql)
        {
            return LooksLikeSql(entityNameOrSql)
                ? "No matching records for query"
                : $"{entityNameOrSql} record does not exist for predicate";
        }

        private static string CreateDiagnosticMessageFor(string? entityNameOrSql, object? predicates)
        {
            return LooksLikeSql(entityNameOrSql)
                ? $"No records found for query:\n${entityNameOrSql}\nwith predicate:\n{Dump(predicates)}"
                : $"{entityNameOrSql} record does not exist for predicate:\n{Dump(predicates)}";
        }


        private static bool LooksLikeSql(string? str)
        {
            return WhiteSpaceRegex.Matches(str ?? string.Empty)
                .Cast<Match>()
                .Any(w => SqlKeyWords.Contains(w.Value));
        }

        private static readonly Regex WhiteSpaceRegex = new("[^\\s]");

        private static readonly HashSet<string> SqlKeyWords = new(
            StringComparer.OrdinalIgnoreCase
        )
        {
            "insert",
            "update",
            "delete",
            "select"
        };

        private static string Dump(object? predicates)
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
            new()
            {
                Formatting = Formatting.Indented,
                PreserveReferencesHandling = PreserveReferencesHandling.All
            };
    }
}