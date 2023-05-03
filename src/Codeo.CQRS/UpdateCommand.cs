namespace Codeo.CQRS
{
    /// <summary>
    /// Convenience: create an update command with only sql
    /// and/or parameters
    /// </summary>
    public abstract class UpdateCommand : Command
    {
        private readonly string _sql;
        private readonly object _parameters;

        /// <summary>
        /// Convenience: create an update command with only sql
        /// </summary>
        /// <param name="sql"></param>
        protected UpdateCommand(
            string sql
        ) : this(sql, null)
        {
        }

        /// <summary>
        /// Convenience: create an update command with only sql
        /// and parameters
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        protected UpdateCommand(
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
            ExecuteUpdate(_sql, _parameters ?? new { });
        }
    }

    /// <inheritdoc />
    public abstract class UpdateCommand<T> : Command<T>
    {
        private readonly string _sql;
        private readonly object _parameters;

        /// <summary>
        /// Convenience: create an update command with only sql,
        /// returning the first result from the first select
        /// in the batch.
        /// </summary>
        /// <param name="sql"></param>
        protected UpdateCommand(
            string sql
        ) : this(sql, null)
        {
        }

        /// <summary>
        /// Convenience: create an update command with only sql
        /// and parameters, returning the first result from the
        /// first select in the batch.
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="parameters"></param>
        protected UpdateCommand(
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
            Result = UpdateGetFirst<T>(
                _sql,
                _parameters ?? new { }
            );
        }
    }
}