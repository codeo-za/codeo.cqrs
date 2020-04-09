using System.Collections.Generic;

namespace Codeo.CQRS.Tests.Models
{
    public class DepartmentWithTags : Department
    {
        public IEnumerable<DepartmentTag> Tags { get; set; }
            = new List<DepartmentTag>();
    }
}