using Codeo.CQRS.Demo.DAO.Models;

namespace Codeo.CQRS.Demo.Infrastructure.Queries.Orders;

public class FetchAllOrders : QueryAsync<List<Order>>
{
    public override Task ExecuteAsync()
    {
        throw new NotImplementedException();
    }

    public override void Validate()
    {
        throw new NotImplementedException();
    }
}