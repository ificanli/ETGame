using CommandLine;
using ET.Server;
using Luban;
using Microsoft.Extensions.DependencyInjection;

namespace ET.Aspire
{
    public class AspireOptions : Singleton<AspireOptions>
    {
        [Option("StartConfig", Required = false, Default = "Localhost")]
        public string StartConfig { get; set; }
        
        [Option("SceneName", Required = false, Default = "WOW", HelpText = "define in SceneType class")]
        public string SceneName { get; set; }
    }
    
    public static class Program
    {
        public static void Main(string[] args)
        {
            IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

            // 命令行参数
            Parser.Default.ParseArguments<AspireOptions>(System.Environment.GetCommandLineArgs())
                    .WithNotParsed(error => throw new Exception($"命令行格式错误! {error}"))
                    .WithParsed((o) => World.Instance.AddSingleton(o));

            // ET配置文件路径 - 智能检测解决方案根目录
            string currentDir = Directory.GetCurrentDirectory();
            Console.WriteLine($"Current working directory: {currentDir}");

            World.Instance.AddSingleton<SceneTypeSingleton, Type>(typeof(SceneType));
            string configBasePath = Path.Combine(currentDir, $"Packages/cn.etetet.startconfig/Bundles/Luban/{AspireOptions.Instance.StartConfig}/Server/Binary");
            string processConfigPath = Path.Combine(configBasePath, "StartProcessConfigCategory.bytes");
            string sceneConfigPath = Path.Combine(configBasePath, "StartSceneConfigCategory.bytes");

            Console.WriteLine($"Reading ET configs from: {configBasePath}");

            World.Instance.AddSingleton(new StartProcessConfigCategory(new ByteBuf(File.ReadAllBytes(processConfigPath))));
            World.Instance.AddSingleton(new StartSceneConfigCategory(new ByteBuf(File.ReadAllBytes(sceneConfigPath))));

            // 为每个进程创建Aspire服务
            foreach ((int processId, StartProcessConfig _) in StartProcessConfigCategory.Instance.GetAll())
            {
                List<StartSceneConfig> processScenes = StartSceneConfigCategory.Instance.GetByProcess(processId);

                // 创建ET进程服务
                string serviceName = $"et-process-{processId}";
                builder.AddExecutable(serviceName, "dotnet", currentDir)
                        .WithArgs("./Bin/ET.App.dll",
                            $"--Process={processId}",
                            $"--SceneName={AspireOptions.Instance.SceneName}",
                            $"--StartConfig={AspireOptions.Instance.StartConfig}")
                        .WithEnvironment("ASPIRE_MANAGED", "true")
                        .WithOtlpExporter();

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