using System;
using Newtonsoft.Json;

namespace Codeo.CQRS.Exceptions
{
    public class EntityDoesNotExistException : Exception
    {
#if DEBUG
        public const bool DEBUG_ENABLED = true;
#else
        public const bool DEBUG_ENABLED = false;
#endif
        public string EntityName { get; set; }
        public object Predicates { get; set; }

        public EntityDoesNotExistException(string entityName, object predicates)
            : base(CreateMessageFor(entityName, predicates))
        {
            EntityName = entityName;
            Predicates = predicates;
        }

        public static string CreateMessageFor(
            string entityName,
            object predicates
        )
        {
#if DEBUG
            return $"{entityName} record does not exist for predicate:\n{Dump(predicates)}";
#else
            return $"{entityName} record does not exist for predicate";
#endif
        }

#if DEBUG
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
#endif
    }
}