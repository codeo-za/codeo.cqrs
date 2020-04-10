using System;

namespace Codeo.CQRS.Tests.Models
{
    public class Person : IEntity
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public bool Enabled { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public DateTime Created { get; set; }
    }
}