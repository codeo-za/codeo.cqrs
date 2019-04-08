using System.Collections.Generic;
using PeanutButter.Utils;

namespace Codeo.CQRS.Tests.Commands
{
    public class CreatePeople : Command<IEnumerable<int>>
    {
        public IEnumerable<string> Names { get; }

        public CreatePeople(params string[] names)
        {
            Names = names;
        }

        public override void Execute()
        {
            ValidateTransactionScope();
            var ids = new List<int>();
            Names.ForEach(name =>
            {
                var id = CommandExecutor.Execute(new CreatePerson(name));
                ids.Add(id);
            });
            Result = ids;
        }

        public override void Validate()
        {
            ValidateTransactionScope();
        }
    }
}