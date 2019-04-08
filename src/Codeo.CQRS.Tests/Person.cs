using System;

namespace Codeo.CQRS.Tests
{
    public class Person : IEntity
    {
        public string Name { get; set; }
        public bool Enabled { get; set; }
        public DateTime Created { get; set; }
    }
}