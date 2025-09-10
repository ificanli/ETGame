using System.Text.Json;

IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

// 读取ET配置参数
string startConfigName = args.FirstOrDefault(a => a.StartsWith("--StartConfig="))?.Replace("--StartConfig=", "") ?? "Localhost";
Console.WriteLine($"Using StartConfig: {startConfigName}");

// ET配置文件路径 - 智能检测解决方案根目录
string currentDir = Directory.GetCurrentDirectory();
Console.WriteLine($"Current working directory: {currentDir}");

string configBasePath = Path.Combine(currentDir, "Packages", "cn.etetet.startconfig", "Bundles", "Luban", startConfigName, "Server", "Json");
string processConfigPath = Path.Combine(configBasePath, "StartProcessConfigCategory.json");
string sceneConfigPath = Path.Combine(configBasePath, "StartSceneConfigCategory.json");

Console.WriteLine($"Reading ET configs from: {configBasePath}");

// 读取进程配置
var processConfigs = JsonSerializer.Deserialize<StartProcessConfig[]>(File.ReadAllText(processConfigPath)) ?? Array.Empty<StartProcessConfig>();

// 读取场景配置  
var sceneConfigs = JsonSerializer.Deserialize<StartSceneConfig[]>(File.ReadAllText(sceneConfigPath)) ?? Array.Empty<StartSceneConfig>();

Console.WriteLine($"Found {processConfigs.Length} processes and {sceneConfigs.Length} scenes");

// 按进程分组场景
var scenesByProcess = sceneConfigs.GroupBy(s => s.Process).ToDictionary(g => g.Key, g => g.ToArray());

// 为每个进程创建Aspire服务
foreach (StartProcessConfig processConfig in processConfigs)
{
    int processId = processConfig.Id;
    var processScenes = scenesByProcess.GetValueOrDefault(processId, Array.Empty<StartSceneConfig>());

    if (processScenes.Length == 0)
    {
        Console.WriteLine($"Warning: Process {processId} has no scenes configured");
        continue;
    }


    // 创建ET进程服务
    string serviceName = $"et-process-{processId}";
    var etProcess = builder.AddExecutable(serviceName, "dotnet", currentDir)
            .WithArgs("./Bin/ET.App.dll",
                $"--Process={processId}",
                $"--SceneName=WOW",
                $"--StartConfig={startConfigName}",
                "--Console")
            .WithEnvironment("ASPIRE_MANAGED", "true")
            .WithOtlpExporter();

    // 输出该进程的所有场景信息
    foreach (StartSceneConfig scene in processScenes)
    {
        Console.WriteLine($"  - Scene: {scene.Name} ({scene.SceneType}) Port: {scene.Port}");
    }
}

// 可以添加Redis/MongoDB等外部依赖
// var redis = builder.AddRedis("cache");
// var mongo = builder.AddMongoDB("gamedb");

builder.Build().Run();

// ET配置数据结构
public record StartProcessConfig(int Id, int MachineId, int Port);

public record StartSceneConfig(int Id, int Process, int Zone, string SceneType, string Name, int Port);