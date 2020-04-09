using System.Collections.Generic;
using System.Linq;

namespace Codeo.CQRS.Tests.Models
{
    public class PersonWithDepartments: Person
    {
        public IEnumerable<Department> Departments { get; set; } 
            = new List<Department>();
    }
}