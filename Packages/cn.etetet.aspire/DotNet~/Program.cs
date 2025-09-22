using CommandLine;
using ET.Server;
using Luban;
using Microsoft.Extensions.DependencyInjection;

namespace ET.Aspire
{
    public static class Program
    {
        public static void Main(string[] args)
        {
            IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

            // 命令行参数
            Parser.Default.ParseArguments<Options>(System.Environment.GetCommandLineArgs())
                    .WithNotParsed(error => throw new Exception($"命令行格式错误! {error}"))
                    .WithParsed((o) => World.Instance.AddSingleton(o));

            // ET配置文件路径 - 智能检测解决方案根目录
            string currentDir = Directory.GetCurrentDirectory();
            Console.WriteLine($"Current working directory: {currentDir}");

            World.Instance.AddSingleton<SceneTypeSingleton, Type>(typeof(SceneType));
            string configBasePath = Path.Combine(currentDir,
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
                if (startProcessConfig.InnerIP == "")
                {
                    innerIP = "0.0.0.0";
                }
                string innerPortStr = startProcessConfig.Port.ToString();

                string outerIP = "0.0.0.0";
                string outerPortStr = "0";
                // 进程只有一个Scene，需要设置OuterIP OuterPort
                if (processScenes.Count == 1)
                {
                    StartSceneConfig startSceneConfig = processScenes[0];
                    outerIP = startProcessConfig.OuterIP;
                    if (outerIP == "")
                    {
                        outerIP = "0.0.0.0";
                    }
                    outerPortStr = startSceneConfig.Port.ToString();
                }

                // 为每个副本创建独立的服务
                for (int replicaIndex = 0; replicaIndex < replicasNum; replicaIndex++)
                {
                    string serviceName = replicasNum > 1 ? $"et-process-{processId}-{replicaIndex}" : $"et-process-{processId}";
                    var p = builder.AddExecutable(serviceName, "dotnet", currentDir, "./Bin/ET.App.dll")
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