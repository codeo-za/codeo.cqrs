using System.Collections.Generic;

namespace Codeo.CQRS.Tests.Models
{
    public class PersonWithDepartments: Person
    {
        public IEnumerable<Department> Departments { get; set; } 
            = new List<Department>();
    }
}