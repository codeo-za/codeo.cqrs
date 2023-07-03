using System.Data;
using Codeo.CQRS.Caching;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MySql.Data.MySqlClient;

namespace Codeo.CQRS.Demo;

public static class Container
{
    public static IServiceProvider ConfigureServices(IConfiguration configuration)
    {
        var services = new ServiceCollection();
        services.AddHttpClient();
        services.AddLogging(l => l.AddConsole());
        
        services.AddTransient<IDbConnection, MySqlConnection>(_ => new MySqlConnection(configuration["MySql:DefaultConnection"]));
        services.AddTransient<IQueryExecutor, QueryExecutor>();
        services.AddTransient<ICommandExecutor, CommandExecutor>();
        services.AddTransient<ICache, Cache>();
        
        return services.BuildServiceProvider();
    }
}