using System.Collections.Generic;
using Codeo.CQRS.Tests.Models;

namespace Codeo.CQRS.Tests.Queries
{
    public class FindDepartmentsById : SelectQuery<IEnumerable<Department>>
    {
        public int[] DepartmentIds { get; }

        public FindDepartmentsById(int[] departmentIds)
            : base("select * from departments where id in @departmentIds;",
                new
                {
                    departmentIds
                })
        {
            DepartmentIds = departmentIds;
        }
    }
}