using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace Codeo.CQRS.Exceptions
{
    /// <summary>
    /// Custom exception to handle Common DB exceptions
    /// </summary>
    public class DatabaseException : Exception
    {
        /// <summary>
        /// The type of operation (insert, select, update, delete) being
        /// performed when the error occurred.
        /// </summary>
        public Operation Operation { get; set; }

        /// <summary>
        /// A label or descriptor for the operation to make it easier to
        /// find &amp; identify issues in logs.
        /// </summary>
        public string QueryDescriptor { get; set; }
        /// <summary>
        /// The predicate / parameters for this query, to help determine
        /// why it was unsuccessful.
        /// </summary>
        public object Predicate { get; set; }

        /// <inheritdoc />
        public DatabaseException(
            Operation operation,
            string queryDescriptor,
            object predicate,
            Exception ex
        )
            : base($@"Error executing query in {
                queryDescriptor
            }, Operation: {
                operation
            }, Predicate: {
                SafeSerialize(predicate)
            }, Error: {
                ex.Message
            }", ex)
        {
            Operation = operation;
            QueryDescriptor = queryDescriptor;
            Predicate = predicate;
        }

        private static string SafeSerialize(
            object predicate)
        {
            if (predicate == null)
            {
                return "[NULL]";
            }

            try
            {
                return JsonConvert.SerializeObject(predicate);
            }
            catch
            {
                var members = predicate.GetType().GetProperties()
                    .Aggregate(new Dictionary<string, string>(), (
                        acc,
                        cur) =>
                    {
                        try
                        {
                            acc[cur.Name] = JsonConvert.SerializeObject(cur.GetValue(predicate));
                        }
                        catch (Exception inner)
                        {
                            acc[cur.Name] = $"[error: {inner.Message}]";
                        }

                        return acc;
                    }).Select(kvp => $"\"{kvp.Key}\": {kvp.Value}");
                return string.Join("\n", new[]
                {
                    "{",
                    string.Join(",\n", members),
                    "}"
                });
            }
        }
    }
}