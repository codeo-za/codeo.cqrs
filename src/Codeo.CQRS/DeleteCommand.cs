namespace Codeo.CQRS
{
    public abstract class DeleteCommand : Command
    {
        private readonly string _sql;
        private readonly object _parameters;

        protected DeleteCommand(
            string sql,
            object parameters = null)
        {
            _sql = sql;
            _parameters = parameters;
        }

        public override void Execute()
        {
            ExecuteDelete(_sql, _parameters ?? new { });
        }
    }
    
    public abstract class DeleteCommand<T> : Command<T>
    {
        private readonly string _sql;
        private readonly object _parameters;

        protected DeleteCommand(
            string sql,
            object parameters = null)
        {
            _sql = sql;
            _parameters = parameters;
        }

        public override void Execute()
        {
            Result = DeleteGetFirst<T>(_sql, _parameters ?? new { });
        }
    }
}