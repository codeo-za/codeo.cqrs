using System;
using System.Collections.Generic;
using System.Linq;

namespace Codeo.CQRS
{
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

    public class CommandExecutor : ICommandExecutor
    {
        private IQueryExecutor queryExecutor { get; set; }
        private ICache cache { get; set; }
        
        public CommandExecutor(
            IQueryExecutor queryExecutor,
            ICache cache)
        {
            this.queryExecutor = queryExecutor;
            this.cache = cache;
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

            command.QueryExecutor = command.QueryExecutor ?? this.queryExecutor;
            command.CommandExecutor = command.CommandExecutor ?? this;
            command.Cache = command.Cache ?? cache;
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
