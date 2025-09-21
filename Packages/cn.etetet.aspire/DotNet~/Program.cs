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
            string configBasePath = Path.Combine(
                currentDir, $"Packages/cn.etetet.startconfig/Bundles/Luban/{Options.Instance.StartConfig}/Server/Binary");
            string processConfigPath = Path.Combine(configBasePath, "StartProcessConfigCategory.bytes");
            string sceneConfigPath = Path.Combine(configBasePath, "StartSceneConfigCategory.bytes");
            string machineConfigPath = Path.Combine(configBasePath, "StartMachineConfigCategory.bytes");
            string zoneConfigPath = Path.Combine(configBasePath, "StartZoneConfigCategory.bytes");

            Console.WriteLine($"Reading ET configs from: {configBasePath}");

            World.Instance.AddSingleton(new StartProcessConfigCategory(new ByteBuf(File.ReadAllBytes(processConfigPath))));
            World.Instance.AddSingleton(new StartSceneConfigCategory(new ByteBuf(File.ReadAllBytes(sceneConfigPath))));
            World.Instance.AddSingleton(new StartMachineConfigCategory(new ByteBuf(File.ReadAllBytes(machineConfigPath))));
            World.Instance.AddSingleton(new StartZoneConfigCategory(new ByteBuf(File.ReadAllBytes(zoneConfigPath))));

            if (Options.Instance.Process == 0)
            {
                builder.Services.AddHostedService<SingleProcessHostedService>();
            }
            else
            {
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
                    if (startProcessConfig.Port == 0)
                    {
                        innerPortStr = "{Port:InnerPort}";
                    }
                    
                    string outerIP = startProcessConfig.OuterIP;
                    if (outerIP == "")
                    {
                        outerIP = "0.0.0.0";
                    }
                    
                    string outerPortStr = startProcessConfig.Port.ToString();
                    if (startProcessConfig.Port == 0)
                    {
                        outerPortStr = "{Port:OuterPort}";
                    }


                    // 创建ET进程服务
                    string serviceName = $"et-process-{processId}";
                    builder.AddProject(serviceName, "dotnet", currentDir)
                            .WithArgs("./Bin/ET.App.dll")
                            .WithArgs($"--Process={processId}") // 固定逻辑进程 ID
                            .WithArgs("--ReplicaIndex={ReplicaIndex}") // 把副本索引传给进程
                            .WithArgs($"--SceneName={Options.Instance.SceneName}")
                            .WithArgs($"--StartConfig={Options.Instance.StartConfig}")
                            .WithArgs($"--SingleThread={Options.Instance.SingleThread}")
                            .WithOtlpExporter()
                            .WithArgs($"--InnerIP={innerIP}")
                            .WithArgs($"--InnerPort={innerPortStr}") // <<< Aspire 会把实际分配的端口替换掉
                            .WithArgs($"--OuterIP={innerIP}")
                            .WithArgs($"--OuterPort={outerPortStr}") // <<< Aspire 会把实际分配的端口替换掉
                            .WithEnvironment("ASPIRE_MANAGED", "true")
                            .WithReplicas(replicasNum)
                            .WithEndpoint(port: 40000, // 基准值，Aspire会根据它分配
                                targetPort: null, // null = 让 Aspire 动态挑选
                                scheme: "udp",
                                name: "InnerPort" // 对应 {Port:InnerPort}
                            )
                            .WithEndpoint(port: 10000, // 基准值，Aspire会根据它分配
                                targetPort: null, // null = 让 Aspire 动态挑选
                                scheme: "udp",
                                name: "OuterPort" // 对应 {Port:OuterPort}
                            );

                    // 输出该进程的所有场景信息
                    foreach (StartSceneConfig scene in processScenes)
                    {
                        Console.WriteLine($"  - Scene: {scene.Name} ({scene.SceneType}) Port: {scene.Port}");
                    }
                }
            }
            

            builder.Build().Run();
        }
    }
}
