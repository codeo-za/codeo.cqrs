namespace Codeo.CQRS
{
    public abstract class UpdateCommand : Command
    {
        private readonly string _sql;
        private readonly object _parameters;

        protected UpdateCommand(
            string sql,
            object parameters = null)
        {
            _sql = sql;
            _parameters = parameters;
        }

        public override void Execute()
        {
            ExecuteUpdate(_sql, _parameters ?? new { });
        }
    }
    public abstract class UpdateCommand<T> : Command<T>
    {
        private readonly string _sql;
        private readonly object _parameters;

        protected UpdateCommand(
            string sql,
            object parameters = null)
        {
            _sql = sql;
            _parameters = parameters;
        }

        public override void Execute()
        {
            Result = UpdateGetFirst<T>(
                _sql, 
                _parameters ?? new { }
            );
        }
    }
}