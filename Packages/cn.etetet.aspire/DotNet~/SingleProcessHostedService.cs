using Microsoft.Extensions.Hosting;

namespace ET
{
    public class SingleProcessHostedService : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            Entry.Init();
            
            Init init = new();
            init.Start();
            
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    init.Update();
                    init.LateUpdate();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }

                await Task.Delay(1, stoppingToken); // 控制帧率，避免空转 100% CPU
            }
        }
    }
}