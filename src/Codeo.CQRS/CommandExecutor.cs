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
        private readonly IQueryExecutor _queryExecutor;
        private readonly ICache _cache;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="queryExecutor"></param>
        /// <param name="cache"></param>
        public CommandExecutor(
            IQueryExecutor queryExecutor,
            ICache cache)
        {
            _queryExecutor = queryExecutor;
            _cache = cache;
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

            command.QueryExecutor ??= _queryExecutor;
            command.CommandExecutor ??= this;
            command.Cache ??= _cache;
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
