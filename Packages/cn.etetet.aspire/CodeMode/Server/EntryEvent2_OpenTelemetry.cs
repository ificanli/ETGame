namespace ET.Server
{
    [Event(SceneType.WOW)]
    public class EntryEvent2_InitServer: AEvent<Scene, EntryEvent2>
    {
        protected override async ETTask Run(Scene root, EntryEvent2 args)
        {
            // 检查是否在Aspire环境中运行
            var aspireManaged = Environment.GetEnvironmentVariable("ASPIRE_MANAGED");
            if (string.IsNullOrEmpty(aspireManaged))
            {
                return; // 不在Aspire环境中，跳过OpenTelemetry初始化
            }

            Console.WriteLine("Initializing OpenTelemetry for Aspire integration");

            try
            {
                // 设置环境变量以启用OTLP导出器
                Environment.SetEnvironmentVariable("OTEL_SERVICE_NAME", "ET.App");
                Environment.SetEnvironmentVariable("OTEL_SERVICE_VERSION", "1.0.0");
                Environment.SetEnvironmentVariable("OTEL_RESOURCE_ATTRIBUTES", $"process.pid={Environment.ProcessId},host.name={Environment.MachineName}");
                
                // 如果没有设置OTLP端点，使用默认值
                if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT")))
                {
                    Environment.SetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT", "http://localhost:21028");
                }

                Console.WriteLine("OpenTelemetry environment configured successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to initialize OpenTelemetry: {ex.Message}");
            }
        }
    }
}