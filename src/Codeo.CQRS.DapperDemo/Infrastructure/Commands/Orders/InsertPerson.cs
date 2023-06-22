using System.Data;
using Codeo.CQRS.Demo.DAO.Models;

namespace Codeo.CQRS.Demo.Infrastructure.Commands.Orders;

public class InsertPerson : CommandAsync<long>
{
    private readonly Person _person;

    public InsertPerson(Person person)
    {
        _person = person;
    }
    
    public override async Task ExecuteAsync()
    {
        var sql = "INSERT INTO people (first_name, last_name, age, date_created) values (@FirstName, @LastName, @Age, @DateCreated); SELECT LAST_INSERT_ID();";
        
        var id = SelectFirst<long>(sql, _person);
        
        Result = id;
    }

    public override void Validate()
    {
        if (_person.Age < 1)
        {
            throw new DataException("Invalid value was provided for person age");
        }
    }
}