using Luban;

namespace ET.Server
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

            World.Instance.AddSingleton<Options>();

            Options.Instance.SceneName = Environment.GetEnvironmentVariable("SceneName");
            Options.Instance.StartConfig = Environment.GetEnvironmentVariable("StartConfig") ?? "Localhost";
            Options.Instance.SingleThread = int.Parse(Environment.GetEnvironmentVariable("SingleThread")  ?? "0");

            string currentDirectory = Directory.GetCurrentDirectory();
            string workDir = Path.Combine(currentDirectory, "../../..");
            Console.WriteLine($"Current working directory: {workDir}");


            string configBasePath = Path.Combine(workDir,
                $"Packages/cn.etetet.startconfig/Bundles/Luban/{Options.Instance.StartConfig}/Server/Binary");
            string processConfigPath = Path.Combine(configBasePath, "StartProcessConfigCategory.bytes");
            string sceneConfigPath = Path.Combine(configBasePath, "StartSceneConfigCategory.bytes");
            string machineConfigPath = Path.Combine(configBasePath, "StartMachineConfigCategory.bytes");
            string zoneConfigPath = Path.Combine(configBasePath, "StartZoneConfigCategory.bytes");

            Console.WriteLine($"Reading ET configs from: {configBasePath}");

            World.Instance.AddSingleton(new StartProcessConfigCategory(new ByteBuf(File.ReadAllBytes(processConfigPath))));
            World.Instance.AddSingleton(new StartSceneConfigCategory(new ByteBuf(File.ReadAllBytes(sceneConfigPath))));
            World.Instance.AddSingleton(new StartMachineConfigCategory(new ByteBuf(File.ReadAllBytes(machineConfigPath))));
            World.Instance.AddSingleton(new StartZoneConfigCategory(new ByteBuf(File.ReadAllBytes(zoneConfigPath))));

            // 为每个进程创建Aspire服务
            foreach ((int processId, StartProcessConfig startProcessConfig) in StartProcessConfigCategory.Instance.GetAll())
            {
                int replicasNum = startProcessConfig.Num > 1 ? startProcessConfig.Num : 1;

                List<StartSceneConfig> processScenes = StartSceneConfigCategory.Instance.GetByProcess(processId);

                string innerIP = startProcessConfig.InnerIP;
                string innerPortStr = startProcessConfig.Port.ToString();

                string outerIP = startProcessConfig.OuterIP;
                
                string outerPortStr = "0";
                // 进程只有一个Scene，需要设置Scene OuterPort
                if (processScenes.Count == 1)
                {
                    StartSceneConfig startSceneConfig = processScenes[0];
                    outerPortStr = startSceneConfig.Port.ToString();
                }

                // 为每个副本创建独立的服务
                for (int replicaIndex = 1; replicaIndex <= replicasNum; replicaIndex++)
                {
                    string serviceName = replicasNum > 1 ? $"{startProcessConfig.Name}-{processId}-{replicaIndex}" : $"{startProcessConfig.Name}-{processId}";
                    // workingDirectory使用环境变量指定的路径
                    builder.AddExecutable(serviceName, "dotnet", workDir, "./Bin/ET.App.dll")
                            .WithArgs($"--Process={processId}") // 固定逻辑进程 ID
                            .WithArgs($"--ReplicaIndex={replicaIndex}") // 副本索引
                            .WithArgs($"--SceneName={Options.Instance.SceneName}")
                            .WithArgs($"--StartConfig={Options.Instance.StartConfig}")
                            .WithArgs($"--SingleThread={Options.Instance.SingleThread}")
                            .WithEnvironment("InnerIP", innerIP)
                            .WithEnvironment("InnerPort", innerPortStr)
                            .WithEnvironment("OuterIP", outerIP)
                            .WithEnvironment("OuterPort", outerPortStr)
                            .WithOtlpExporter()
                            .WithEnvironment("ASPIRE_MANAGED", "true");
                }

                // 输出该进程的所有场景信息
                foreach (StartSceneConfig scene in processScenes)
                {
                    Console.WriteLine($"  - Scene: {scene.Name} ({scene.SceneType}) Port: {scene.Port}");
                }
            }

            builder.Build().Run();
        }
    }
}