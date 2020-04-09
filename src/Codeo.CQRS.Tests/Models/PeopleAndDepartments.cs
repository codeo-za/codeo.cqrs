using System.Collections.Generic;
using System.Linq;

namespace Codeo.CQRS.Tests.Models
{
    public class PeopleAndDepartments
    {
        public IEnumerable<Person> People { get; set; }
            = Enumerable.Empty<Person>();
        public IEnumerable<Department> Departments { get; set; }
            = Enumerable.Empty<Department>();
    }
}