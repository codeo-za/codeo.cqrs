using System;

namespace Codeo.CQRS
{
    /// <summary>
    /// Thrown when one or more properties specified to the [Cache] attribute
    /// cannot be found on the target query
    /// </summary>
    public class InvalidCachePropertiesSpecified : Exception
    {
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
                }";
        }
    }
}