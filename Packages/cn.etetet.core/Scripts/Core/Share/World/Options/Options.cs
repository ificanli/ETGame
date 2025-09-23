using CommandLine;

namespace ET
{
    public class Options: Singleton<Options>, ISingletonAwake
    {
        [Option("SceneName", Required = false, Default = "", HelpText = "define in SceneType class")]
        public string SceneName { get; set; }
        
        [Option("StartConfig", Required = false, Default = "Localhost")]
        public string StartConfig { get; set; }

        [Option("Process", Required = false, Default = 1)]
        public int Process { get; set; }
        
        [Option("ReplicaIndex", Required = false, Default = 0)]
        public int ReplicaIndex { get; set; }
        
        [Option("LogLevel", Required = false, Default = 0)]
        public int LogLevel { get; set; }
        
        [Option("Console", Required = false, Default = 0)]
        public int Console { get; set; }
        
        [Option("SingleThread", Required = false, Default = 0)]
        public int SingleThread { get; set; }

        public void Awake()
        {
        }
    }
}