namespace Codeo.CQRS
{
    /// <summary>
    /// Convenience: provide a simple deleting command
    /// which only requires providing sql and parameters
    /// </summary>
    public abstract class DeleteCommand : Command
    {
        private readonly string _sql;
        private readonly object _parameters;

        /// <summary>
        /// Convenience: delete with sql only.
        /// </summary>
        /// <param name="sql"></param>
        protected DeleteCommand(
            string sql
        ) : this(sql, null)
        {
        }

        /// <summary>
        /// Convenience: delete with sql and parameters
        /// only.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        protected DeleteCommand(
            string sql,
            object parameters)
        {
            _sql = sql;
            _parameters = parameters;
        }

        /// <inheritdoc />
        public override void Execute()
        {
            ExecuteDelete(_sql, _parameters ?? new { });
        }
    }

    /// <summary>
    /// Convenience: provide a simple deleting command
    /// which only requires providing sql and parameters,
    /// returning a single value from the first select
    /// in the provided sql batch.
    /// </summary>
    public abstract class DeleteCommand<T> : Command<T>
    {
        private readonly string _sql;
        private readonly object _parameters;

        /// <summary>
        /// Convenience: delete with sql only, returning
        /// the single value from the first select in
        /// the sql batch.
        /// </summary>
        /// <param name="sql"></param>
        protected DeleteCommand(
            string sql
        ) : this(sql, null)
        {
        }

        /// <summary>
        /// Convenience: delete with sql and parameters,
        /// returning the single value from the first select
        /// in the sql batch.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        protected DeleteCommand(
            string sql,
            object parameters
        )
        {
            _sql = sql;
            _parameters = parameters;
        }

        /// <inheritdoc />
        public override void Execute()
        {
            Result = DeleteGetFirst<T>(_sql, _parameters ?? new { });
        }
    }
}