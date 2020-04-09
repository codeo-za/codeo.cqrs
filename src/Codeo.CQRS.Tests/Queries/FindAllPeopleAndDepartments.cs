using Codeo.CQRS.Tests.Models;

namespace Codeo.CQRS.Tests.Queries
{
    public class FindAllPeopleAndDepartments
        : Query<PeopleAndDepartments>
    {
        public override void Execute()
        {
            SelectMulti(
                "select * from people; select * from departments;",
                reader =>
                {
                    Result = new PeopleAndDepartments()
                    {
                        People = reader.Read<Person>(),
                        Departments = reader.Read<Department>()
                    };
                });
        }
    }
}