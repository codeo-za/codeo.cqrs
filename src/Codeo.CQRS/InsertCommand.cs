using System;
using Codeo.CQRS.Exceptions;

namespace Codeo.CQRS
{
    /// <summary>
    /// Provides a convenience base class for inserts
    /// </summary>
    public abstract class InsertCommand : Command
    {
        private readonly string _sql;
        private readonly object _parameters;

        /// <summary>
        /// Convenience: insert with only the provided sql
        /// </summary>
        /// <param name="sql"></param>
        protected InsertCommand(string sql) : this(sql, null)
        {
        }

        /// <summary>
        /// Convenience: insert with only the provided sql and parameters
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        protected InsertCommand(
            string sql,
            object parameters
        )
        {
            _sql = sql;
            _parameters = parameters;
        }

        /// <inheritdoc />
        public sealed override void Execute()
        {
            ExecuteInsert(_sql, _parameters ?? new { });
        }
    }

    /// <summary>
    /// Provides a convenience base class for inserts
    /// with a single return value
    /// </summary>
    /// <typeparam name="T">type of return value</typeparam>
    public abstract class InsertCommand<T> : Command<T>
    {
        private readonly string _sql;
        private readonly object _parameters;

        /// <summary>
        /// Convenience: insert with only the provided sql,
        /// returning the value from the first select in
        /// the batch
        /// </summary>
        /// <param name="sql"></param>
        protected InsertCommand(
            string sql
        ) : this(sql, null)
        {
        }

        /// <summary>
        /// Convenience: insert with only the provided sql
        /// and parameters, returning the value from the
        /// first select in the batch
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        protected InsertCommand(
            string sql,
            object parameters
        )
        {
            _sql = sql;
            _parameters = parameters;
        }

        /// <inheritdoc />
        public sealed override void Execute()
        {
            try
            {
                Result = InsertGetFirst<T>(_sql, _parameters ?? new { });
            }
            catch (EntityNotFoundException ex)
            {
                throw new InvalidOperationException(
                    $"Unable to InsertGetFirst for {GetType()}. Perhaps you forget to select the last inserted id in your query:\n{_sql}",
                    ex
                );
            }
        }
    }
}