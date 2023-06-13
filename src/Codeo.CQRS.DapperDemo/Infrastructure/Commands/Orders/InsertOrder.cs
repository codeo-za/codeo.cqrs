using Codeo.CQRS.Demo.DAO.Models;

namespace Codeo.CQRS.Demo.Infrastructure.Commands.Orders;

public class InsertOrder : CommandAsync<long>
{
    private readonly Order _order;

    public InsertOrder(Order order)
    {
        _order = order;
    }

    public override Task ExecuteAsync()
    {
        throw new NotImplementedException();
    }

    public override void Validate()
    {
        if (_order.PersonId is 0 || _order.StoreId is 0)
        {
            throw new InvalidDataException("The PersonId and StoreId can not be 0");
        }
    }
}