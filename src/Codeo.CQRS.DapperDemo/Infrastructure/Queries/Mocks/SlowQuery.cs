using System.Diagnostics;

namespace Codeo.CQRS.Demo.Infrastructure.Queries.Mocks;

public class SlowQuery : QueryAsync<int>
{
    public override async Task ExecuteAsync()
    {
        var httpClient = GetRequiredService<HttpClient>();
        var timer = new Stopwatch();
        timer.Start();
        
        var task1 = Task.Run(() => httpClient.GetAsync("https://hub.dummyapis.com/delay?seconds=5"));
        var task2 = Task.Run(() => httpClient.GetAsync("https://hub.dummyapis.com/delay?seconds=5"));

        var r = await Task.WhenAll(task1, task2);
        timer.Stop();

        if (timer.Elapsed >= TimeSpan.FromSeconds(5.5))
        {
            Result = 0;
            return;
        }

        Result = 1;
    }

    public override void Validate()
    {
        // n/a
    }
}