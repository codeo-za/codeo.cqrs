namespace Codeo.CQRS.Demo.DAO.Models;

/// <summary>
/// create table store
/// (
///     id bigint primary key auto_increment,
///     name varchar(255),
///     location varchar(255),
///     date_created datetime not null
/// )
/// </summary>
public class Store
{
    public Store()
    {
        DateCreated = DateTime.UtcNow;
    }
    
    public long Id { get; set; }
    public string? Name { get; set; }
    public string? Location { get; set; }
    public DateTime DateCreated { get; set; }
}