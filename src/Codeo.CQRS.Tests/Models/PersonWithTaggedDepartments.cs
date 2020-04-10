using System.Collections.Generic;

namespace Codeo.CQRS.Tests.Models
{
    public class PersonWithTaggedDepartments : Person
    {
        public IEnumerable<DepartmentWithTags> Departments { get; set; }
            = new List<DepartmentWithTags>();
    }
}