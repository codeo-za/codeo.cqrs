namespace Codeo.CQRS.Demo.DAO.Models;

/// <summary>
/// Person POCO: Create in MySql with...
/// create table orders
/// (
///     id bigint primary key auto_increment,
///     person_id bigint,
///     store_id bigint,
///     description text,
///     price double,
///     date_created datetime not null
/// );
/// </summary>
public class Order
{
    public Order()
    {
        DateCreated = DateTime.UtcNow;
    }
    
    public long Id { get; set; }
    public long PersonId { get; set; } // ref person(id)
    public long StoreId { get; set; } // ref store(id)
    public string? Description { get; set; }
    public double Price { get; set; }
    public DateTime DateCreated { get; set; }
}