using System;

namespace Codeo.CQRS
{
    /// <summary>
    /// Thrown when one or more properties specified to the [Cache] attribute
    /// cannot be found on the target query
    /// </summary>
    public class InvalidCachePropertiesSpecified : Exception
    {
        private const string HelpfulHint = @"
For members to be considered for cache keys, they must be:
- properties
- on the instance (no statics)
- any access modifier (public, protected, private, internal)
  - but prefer public, read-only props: testing later will be easier
";
        public InvalidCachePropertiesSpecified(
            params string[] propertyNames
        ) : base(
            GenerateMessageFor(propertyNames)
        )
        {
        }

        private static string GenerateMessageFor(params string[] propertyNames)
        {
            return $@"Specified cache property{
                    (propertyNames.Length == 1 ? "" : "s")
                } not found: {
                    string.Join(", ", propertyNames)
                }{HelpfulHint}";
        }
    }
}