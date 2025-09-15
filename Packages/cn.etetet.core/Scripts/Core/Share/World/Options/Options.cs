using CommandLine;

namespace ET
{
    public class Options: Singleton<Options>, ISingletonAwake
    {
        [Option("SceneName", Required = false, Default = "", HelpText = "define in SceneType class")]
        public string SceneName { get; set; }
        
        [Option("WatcherStartSceneName", Required = false, Default = "", HelpText = "define in SceneType class")]
        public string WatcherStartSceneName { get; set; }

        [Option("StartConfig", Required = false, Default = "Localhost")]
        public string StartConfig { get; set; }

        [Option("Process", Required = false, Default = 1)]
        public int Process { get; set; }
        
        [Option("IP", Required = false, Default = 0)]
        public int IP { get; set; } 
        
        [Option("Port", Required = false, Default = 0)]
        public int Port { get; set; } 
        
        [Option("Develop", Required = false, Default = 0, HelpText = "develop mode, 0正式 1开发 2压测")]
        public int Develop { get; set; }

        [Option("LogLevel", Required = false, Default = 0)]
        public int LogLevel { get; set; }
        
        [Option("Console", Required = false, Default = 0)]
        public int Console { get; set; }
        
        [Option("SingleThread", Required = false, Default = 0)]
        public int SingleThread { get; set; }
        
        public Address Address
        {
            get
            {
                return new Address(this.IP, this.Port);
            }
            set 
            {
                this.IP = value.IP;
                this.Port = value.Port;
            }
        }

        public void Awake()
        {
        }
    }
}