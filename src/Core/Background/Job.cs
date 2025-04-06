namespace ReelGrab.Core.Background;

public abstract class Job : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (true)
        {
            if(stoppingToken.IsCancellationRequested)
            {
                return;
            }
            try
            {
                await RunAsync(stoppingToken);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e.StackTrace);
            }
            finally {
                await Task.Delay(Interval, stoppingToken);
            }
        }
    }

    public abstract TimeSpan Interval { get; }

    public abstract Task RunAsync(CancellationToken stoppingToken);
}