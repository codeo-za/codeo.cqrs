using System;

namespace Codeo.CQRS.Tests
{
    public class PersonLike
    {
        public string Name { get; set; }
        public bool Enabled { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime Created { get; set; }
    }
}