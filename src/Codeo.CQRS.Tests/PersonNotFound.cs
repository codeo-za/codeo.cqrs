using System;

namespace Codeo.CQRS.Tests
{
    public class PersonNotFound : Exception
    {
        public PersonNotFound(int id) : base($"Person not found by id: {id}")
        {
        }
    }
}