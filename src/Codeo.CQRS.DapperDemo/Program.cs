// See https://aka.ms/new-console-template for more information


using System.Data;
using System.Text.Json;
using Codeo.CQRS;
using Codeo.CQRS.Demo;
using Codeo.CQRS.Demo.DAO.Models;
using Codeo.CQRS.Demo.Infrastructure.Commands.Orders;
using Codeo.CQRS.Demo.Infrastructure.Queries.Orders;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using static Codeo.CQRS.Demo.Container;

var configurationRoot = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", false, true)
    .Build();

var serviceProvider = ConfigureServices(configurationRoot);
var connectionFactory = new ConnectionFactory(serviceProvider.GetRequiredService<IDbConnection>());

Fluently
    .Configure(c =>
    {
        c.WithConnectionFactory(connectionFactory);
        c.WithServiceProvider(serviceProvider);
        c.WithSnakeCaseMappingEnabled();
    });

var commandExecutor = serviceProvider.GetRequiredService<ICommandExecutor>();
var queryExecutor = serviceProvider.GetRequiredService<IQueryExecutor>();

var id = await commandExecutor.ExecuteAsync(new InsertPerson(new Person
{
    FirstName = "John",
    LastName = "Doe",
    Age = 21
}));

var result = await queryExecutor.ExecuteAsync(new FetchAllPeople());

Console.WriteLine(JsonSerializer.Serialize(result));

Console.WriteLine(id);

    