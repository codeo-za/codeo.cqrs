namespace Codeo.CQRS
{
    /// <summary>
    /// Provides a convenience base class for inserts
    /// </summary>
    public abstract class InsertCommand : Command
    {
        private readonly string _sql;
        private readonly object _parameters;

        protected InsertCommand(
            string sql,
            object parameters = null)
        {
            _sql = sql;
            _parameters = parameters;
        }

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

        protected InsertCommand(
            string sql,
            object parameters = null)
        {
            _sql = sql;
            _parameters = parameters;
        }

        public sealed override void Execute()
        {
            Result = InsertGetFirst<T>(_sql, _parameters ?? new { });
        }
    }
}