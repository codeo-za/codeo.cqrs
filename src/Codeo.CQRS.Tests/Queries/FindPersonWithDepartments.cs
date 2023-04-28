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
            Result = SelectOneToMany<PersonWithDepartments, Department>(
                @"select _p.*, _d.* from people _p
                    inner join departments_people _dp
                        on _p.id = _dp.person_id
                    inner join departments _d
                        on _dp.department_id = _d.id
                    where
                        _p.id = @PersonId;",
                new
                {
                    PersonId
                },
                idFinder: p => p.Id,
                collectionFinder: p => p.Departments as List<Department>
            ).FirstOrDefault();
        }

        public override void Validate()
        {
        }
    }
}