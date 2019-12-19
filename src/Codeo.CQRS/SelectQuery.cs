using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

// ReSharper disable StaticMemberInGenericType

namespace Codeo.CQRS
{
    public enum SingleSelectMissingStrategies
    {
        Throw,
        ReturnNull
    }

    /// <summary>
    /// Provides a convenient base class for simple select queries,
    /// ie queries which can be broken down into a single select with
    /// sql and perhaps some parameters
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class SelectQuery<T> : Query<T>
    {
        private readonly SingleSelectMissingStrategies _missingStrategy;
        private readonly string _sql;
        private readonly object _parameters;

        protected SelectQuery(
            string sql) : this(sql, null)
        {
        }

        protected SelectQuery(
            string sql,
            object parameters)
        {
            _sql = sql;
            _parameters = parameters;
        }

        protected SelectQuery(
            string sql,
            object parameters,
            SingleSelectMissingStrategies missingStrategy) : this(sql, parameters)
        {
            if (IsEnumerableResult)
            {
                throw new ArgumentOutOfRangeException(
                    $"{nameof(missingStrategy)} is only valid for single-result queries"
                );
            }

            _missingStrategy = missingStrategy;
        }


        public override void Execute()
        {
            if (IsEnumerableResult)
            {
                PerformTypesCollectionQuery();
            }
            else
            {
                PerformSingleItemQuery();
            }
        }

        private void PerformSingleItemQuery()
        {
            Result = _missingStrategy == SingleSelectMissingStrategies.Throw
                ? SelectFirst<T>(_sql, _parameters)
                : SelectMany<T>(_sql, _parameters).FirstOrDefault();
        }

        private void PerformTypesCollectionQuery()
        {
            var selectResult = TypedCollectionSelectMethod
                .Invoke(this, new[] { _sql, _parameters });
            Result = typeof(T).IsArray
                ? ConvertToArray(selectResult, CollectionItemType)
                : (T) selectResult;
        }

        private T ConvertToArray(object result, Type itemType)
        {
            // ReSharper disable once PossibleNullReferenceException (using nameof, this method must exist)
            var generic = GetType()
                .GetMethod(
                    nameof(ConvertToArrayGeneric),
                    BindingFlags.NonPublic | BindingFlags.Static
                ).MakeGenericMethod(itemType);
            return (T) (generic.Invoke(null, new[] { result }));
        }

        private static TItem[] ConvertToArrayGeneric<TItem>(IEnumerable<TItem> collection)
        {
            return collection.ToArray();
        }

        private static readonly bool IsEnumerableResult = DetermineIfResultTypeIsEnumerable();

        private static bool DetermineIfResultTypeIsEnumerable()
        {
            return typeof(T) != typeof(string) &&
                typeof(T).ImplementsEnumerableGenericType();
        }

        private static readonly MethodInfo SelectListMethodInfo
            = typeof(Query<T>).GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .FirstOrDefault(mi => mi.Name == (nameof(SelectMany)) &&
                    mi.HasParameters(typeof(string), typeof(object)));

        private static readonly Type CollectionItemType = typeof(T).GetCollectionItemType();

        private static readonly MethodInfo TypedCollectionSelectMethod
            = IsEnumerableResult
                ? SelectListMethodInfo.MakeGenericMethod(
                    typeof(T).GetCollectionItemType()
                )
                : typeof(SelectQuery<T>).GetMethod(
                    nameof(ThrowWhenAttemptToSelectManyOnNonEnumerable),
                    BindingFlags.Static | BindingFlags.NonPublic
                );

        private static void ThrowWhenAttemptToSelectManyOnNonEnumerable(
            string sql,
            object parameters)
        {
            throw new InvalidOperationException(
                string.Join("\n",
                    $"An attempt was made to perform a collection query against {typeof(T)}",
                    "This should never happen -- please report it if it does",
                    "If you're seeing this error, it's better than a strange NullReference",
                    "exception, but does mean that something within SelectQuery<T> needs fixing"
                )
            );
        }
    }
}