using System.Collections.Generic;
using System.Linq;
using Codeo.CQRS.Tests.Models;
using PeanutButter.Utils;

namespace Codeo.CQRS.Tests.Queries
{
    public class FindPersonWithDepartmentsAndTags 
        : Query<PersonWithTaggedDepartments>
    {
        public int PersonId { get; }

        public FindPersonWithDepartmentsAndTags(
            int personId)
        {
            PersonId = personId;
        }

        public override void Execute()
        {
            var results = new List<PersonWithTaggedDepartments>();
            SelectMulti<
                Person, Department, DepartmentTag, PersonWithTaggedDepartments>(
                @"select _p.*, _d.*, _dt.* from people _p
                    inner join departments_people _dp
                        on _p.id = _dp.person_id
                    inner join departments _d
                        on _dp.department_id = _d.id
                    left join departments_tags _dt
                        on _d.id = _dt.department_id
                    where
                        _p.id = @PersonId;",
                new
                {
                    PersonId
                },
                (person, department, tag) =>
                {
                    // TODO: rather use a dictionary of known items
                    //    as FirstOrDefault traverses the entire collection
                    //    -> for now, this is performant enough
                    var resultItem = results.FirstOrDefault(p => p.Id == person.Id)
                        ?? CreateResultItemFor(results, person);
                    var departments = resultItem.Departments as IList<DepartmentWithTags>;
                    var dept = departments.FirstOrDefault(d => d.Id == department.Id)
                        ?? AddDepartmentFor(department, departments);
                    var tags = dept.Tags as IList<DepartmentTag>;
                    tags.Add(tag);
                    return null; // we're collecting in here, so no need for a result            
                }
            );
            Result = results.FirstOrDefault();
        }

        public override void Validate()
        {
        }

        private DepartmentWithTags AddDepartmentFor(
            Department department, 
            IList<DepartmentWithTags> departments)
        {
            var result = new DepartmentWithTags();
            department.CopyPropertiesTo(result);
            departments.Add(result);
            return result;
        }

        private static PersonWithTaggedDepartments CreateResultItemFor(
            List<PersonWithTaggedDepartments> results,
            Person person)
        {
            var result = new PersonWithTaggedDepartments();
            // comes out of PeanutButter.Utils if you're looking for this
            // functionality; or perhaps you have your own .Bind method somewhere?
            person.CopyPropertiesTo(result);
            results.Add(result);
            return result;
        }
    }
}