using System.Collections.Generic;
using System.Linq;
using Codeo.CQRS.Tests.Models;

namespace Codeo.CQRS.Tests.Queries
{
    public class FindPersonWithDepartments : Query<PersonWithDepartments>
    {
        public int PersonId { get; }

        public FindPersonWithDepartments(int personId)
        {
            PersonId = personId;
        }

        public override void Execute()
        {
            var mappedRows = SelectMulti<PersonWithDepartments, Department, PersonWithDepartments>(
                @"select _p.*, _d.* from people _p
                    inner join departments_people _dp
                        on _p.id = _dp.person_id
                    inner join departments _d
                        on _dp.department_id = _d.id
                    where
                        _p.id = @PersonId;",
                (person, department) =>
                {
                    var depts = person.Departments as List<Department>;
                    depts.Add(department);
                    return person;
                },
                new
                {
                    PersonId
                }
            );

            Result = mappedRows.Aggregate(
                null as PersonWithDepartments,
                (acc, cur) =>
                {
                    if (acc is null)
                    {
                        return cur;
                    }

                    var depts = acc.Departments as List<Department>;
                    depts.AddRange(cur.Departments);
                    return acc;
                });
        }
    }
}