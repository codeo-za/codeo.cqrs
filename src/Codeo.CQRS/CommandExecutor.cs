using System;
using System.Collections.Generic;
using System.Linq;
using Codeo.CQRS.Caching;

namespace Codeo.CQRS
{
    /// <summary>
    /// Executes commands
    /// </summary>
    public interface ICommandExecutor
    {
        /// <summary>
        /// Executes the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        void Execute(Command command);

        /// <summary>
        /// Executes the specified command.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        T Execute<T>(Command<T> command);

        /// <summary>
        /// Executes the specified commands.
        /// </summary>
        /// <param name="commands">The commands.</param>
        void Execute(IEnumerable<Command> commands);
    }

    /// <inheritdoc />
    public class CommandExecutor : ICommandExecutor
    {
        private readonly Func<IQueryExecutor> _queryExecutorFactory;
        private readonly Func<ICache> _cacheFactory;

        /// <summary>
        /// Creates the CommandExecutor with
        /// a QueryExecutor to provide to sub-queries
        /// and a Cache service
        /// </summary>
        /// <param name="queryExecutor"></param>
        /// <param name="cache"></param>
        public CommandExecutor(
            IQueryExecutor queryExecutor,
            ICache cache
        ): this(() => queryExecutor, () => cache)
        {
        }

        /// <summary>
        /// Creates the CommandExecutor with factories for
        /// the QueryExecutor to provide to sub-queries and
        /// the Cache implementation
        /// </summary>
        /// <param name="queryExecutorFactory"></param>
        /// <param name="cacheFactory"></param>
        public CommandExecutor(
            Func<IQueryExecutor> queryExecutorFactory,
            Func<ICache> cacheFactory
        )
        {
            _queryExecutorFactory = queryExecutorFactory;
            _cacheFactory = cacheFactory;
        }

        /// <summary>
        /// Executes the specified command.
        /// </summary>
        /// <param name="command">The command.</param>
        public void Execute(Command command)
        {
            if (command == null)
            {
                throw new ArgumentNullException(nameof(command));
            }

            command.QueryExecutor ??= _queryExecutorFactory();
            command.CommandExecutor ??= this;
            command.Cache ??= _cacheFactory();
            command.Validate();
            command.Execute();
        }

        /// <summary>
        /// Executes the specified command.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="command">The command.</param>
        /// <returns></returns>
        public T Execute<T>(Command<T> command)
        {
            Execute(command as Command);
            return command.Result;
        }

        /// <summary>
        /// Executes the specified commands.
        /// </summary>
        /// <param name="commands">The commands.</param>
        public void Execute(IEnumerable<Command> commands)
        {
            var commandsArray = commands as Command[] ?? commands.ToArray();
            if (commands == null)
            {
                throw new ArgumentNullException(nameof(commands));
            }

            foreach (var command in commandsArray)
            {
                Execute(command);
            }
        }
    }
}