using System.Collections.Generic;
using Codeo.CQRS.Tests.Models;

namespace Codeo.CQRS.Tests.Queries
{
    public class CreateDepartment : InsertCommand<int>
    {
        public string Name { get; }

        public CreateDepartment(string name)
            : base(
                @"insert into departments (name) values (@name);
                select LAST_INSERT_ID() as id",
                new
                {
                    name
                })
        {
            Name = name;
        }
    }

    public class CreateTagForDepartment : InsertCommand<int>
    {
        public int DepartmentId { get; }
        public string Tag { get; }

        public CreateTagForDepartment(
            int departmentId,
            string tag
        ) : base(
            @"insert into departments_tags (department_id, tag)
                values (@departmentId, @tag);
                select LAST_INSERT_ID() as id;",
            new
            {
                departmentId,
                tag
            })
        {
            DepartmentId = departmentId;
            Tag = tag;
        }
    }

    public class FindTagsById : SelectQuery<IEnumerable<DepartmentTag>>
    {
        public int[] Ids { get; }

        public FindTagsById(int[] ids)
            : base(
                @"select * from departments_tags where id in @ids;
                select LAST_INSERT_ID() as id;",
                new
                {
                    ids
                }
            )
        {
            Ids = ids;
        }
    }
}