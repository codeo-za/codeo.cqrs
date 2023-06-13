namespace Codeo.CQRS.Demo.DAO.Models;

/// <summary>
/// Person POCO: Create in MySql with...
/// create table people
/// (
///     id           bigint primary key auto_increment,
///     first_name   varchar(255) not null,
///     last_name    varchar(255) null,
///     age          int not null,
///     date_created datetime not null
/// );
/// </summary>
public class Person
{
    public Person()
    {
        DateCreated = DateTime.UtcNow;
    }
    
    public long Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public int Age { get; set; }
    public DateTime DateCreated { get; set; }
}