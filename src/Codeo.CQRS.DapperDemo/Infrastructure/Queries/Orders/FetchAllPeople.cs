using Codeo.CQRS.Demo.DAO.Models;
using Dapper;
using MySqlX.XDevAPI.Common;

namespace Codeo.CQRS.Demo.Infrastructure.Queries.Orders;

public class FetchAllPeople : QueryAsync<List<Person>>
{
    public override async Task ExecuteAsync()
    {
        var result = await DbConnection.QueryAsync<Person>("select * from people;");
        
        Result = result.ToList();
    }

    public override void Validate()
    {
        
    }
}