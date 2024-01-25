using System;
using System.Collections.Generic;

namespace Codeo.CQRS.Internal
{
    internal static class StringExtensions
    {
        /// <summary>
        /// Easy way to produce a text list from a collection of items, eg
        /// [ "cat", "dog", "cow" ]
        /// becomes
        /// - cat
        /// - dog
        /// - cow
        /// </summary>
        /// <param name="input"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public static string AsTextList<T>(
            this IEnumerable<T> input
        )
        {
            return input.AsTextList(DEFAULT_LIST_ITEM_MARKER);
        }

        private const string DEFAULT_LIST_ITEM_MARKER = "- ";
        private const string DEFAULT_EMPTY_LIST_TEXT = "<empty>";


        /// <summary>
        /// Easy way to produce a text list from a collection of items with
        /// a provided item marker, eg if the item marker is '* '
        /// [ "cat", "dog", "cow" ]
        /// becomes
        /// * cat
        /// * dog
        /// * cow
        /// </summary>
        /// <param name="input"></param>
        /// <param name="itemMarker"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        internal static string AsTextList<T>(
            this IEnumerable<T> input,
            string itemMarker
        )
        {
            return input.AsTextList(itemMarker, DEFAULT_EMPTY_LIST_TEXT);
        }

        /// <summary>
        /// Easy way to produce a text list from a collection of items with
        /// a provided item marker, eg if the item marker is '* '
        /// [ "cat", "dog", "cow" ]
        /// becomes
        /// * cat
        /// * dog
        /// * cow
        /// </summary>
        /// <param name="input"></param>
        /// <param name="itemMarker"></param>
        /// <param name="whenEmpty">Returns this string when the collection is empty</param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        internal static string AsTextList<T>(
            this IEnumerable<T> input,
            string itemMarker,
            string whenEmpty
        )
        {
            var result = new List<string>();
            foreach (var item in input ?? Array.Empty<T>())
            {
                result.Add($"{itemMarker}{item}");
            }

            return result.Count > 0
                ? $"{string.Join("\n", result)}"
                : whenEmpty;
        }

    }
}