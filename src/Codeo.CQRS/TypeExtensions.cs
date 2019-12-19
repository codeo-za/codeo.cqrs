using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Codeo.CQRS
{
    internal static class TypeExtensions
    {
        internal static Type GetCollectionItemType(this Type collectionType)
        {
            if (collectionType.IsArray)
            {
                return collectionType.GetElementType();
            }

            if (collectionType.IsGenericType())
            {
                return collectionType.GenericTypeArguments[0];
            }

            var enumerableInterface = collectionType.TryGetEnumerableInterface();
            return enumerableInterface != null
                ? enumerableInterface.GenericTypeArguments[0]
                : null;
        }
        
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
        
        internal static bool IsGenericOf(this Type t, Type genericTest)
        {
            return t.IsGenericType && t.GetGenericTypeDefinition() == genericTest;
        }
        
        internal static bool IsGenericType(this Type t)
        {
            return t
#if NETSTANDARD
                   .GetTypeInfo()
#endif
                   .IsGenericType;
        }
        
        internal static bool ImplementsEnumerableGenericType(this Type t)
        {
            return t.IsGenericOfIEnumerable() || 
                t.TryGetEnumerableInterface() is object;
        }

        internal static bool HasParameters(
            this MethodInfo mi,
            params Type[] desiredParameterTypes)
        {
            var actualParameterTypes = mi.GetParameters()
                .Select(p => p.ParameterType)
                .ToArray();
            return actualParameterTypes.Length == desiredParameterTypes.Length &&
                desiredParameterTypes.Zip(
                    actualParameterTypes, 
                    Tuple.Create
                ).Aggregate(true, (acc, cur) => acc && cur.Item1 == cur.Item2);
        }
    }
}