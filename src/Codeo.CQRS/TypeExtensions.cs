using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Codeo.CQRS
{
    internal static class TypeExtensions
    {
        /// <summary>
        /// Attempts to get the item type of a colleciton
        /// </summary>
        /// <param name="collectionType">Type to inspect</param>
        /// <returns>Item type, if it can be found, or null</returns>
        internal static Type GetCollectionItemType(this Type collectionType)
        {
            if (collectionType.IsArray)
                return collectionType.GetElementType();
            if (collectionType.IsGenericType())
                return collectionType.GenericTypeArguments[0];
            var enumerableInterface = collectionType.TryGetEnumerableInterface();
            if (enumerableInterface != null)
                return enumerableInterface.GenericTypeArguments[0];
            return null;
        }
        /// <summary>
        /// Attempts to get the implemented Generic IEnumerable interface for a type, if possible
        /// </summary>
        /// <param name="srcType">Type to search for the interface</param>
        /// <returns>Generic IEnumerable type implemented if found or null otherwise</returns>
        internal static Type TryGetEnumerableInterface(this Type srcType)
        {
            return srcType.IsGenericOfIEnumerable()
                ? srcType
                : srcType.GetInterfaces().FirstOrDefault(IsGenericOfIEnumerable);
        }
        
        internal static bool IsGenericOfIEnumerable(this Type arg)
        {
            return arg.IsGenericOf(typeof(IEnumerable<>));
        }
        /// <summary>
        /// Tests if a type is a generic of a given generic type (eg typeof(List&lt;&gt;))
        /// </summary>
        /// <param name="t">type to operate on</param>
        /// <param name="genericTest">type to test against (eg typeof(List&lt;&gt;))</param>
        /// <returns>True if the input type is a match, false otherwise</returns>
        internal static bool IsGenericOf(this Type t, Type genericTest)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == genericTest;
        }
        /// <summary>
        /// Provides an extension method mimicking the full framework
        /// IsGenericType for a single point of code usage
        /// </summary>
        /// <param name="t"></param>
        /// <returns></returns>
        internal static bool IsGenericType(this Type t)
        {
            return t
#if NETSTANDARD
                   .GetTypeInfo()
#endif
                   .IsGenericType;
        }
    }
}