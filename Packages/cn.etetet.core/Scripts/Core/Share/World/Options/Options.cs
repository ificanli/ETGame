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
        
        [Option("InnerIP", Required = false, Default = "0.0.0.0")]
        public string InnerIP { get; set; } 
        
        [Option("InnerPort", Required = false, Default = 0)]
        public int InnerPort { get; set; } 
        
        [Option("OuterIP", Required = false, Default = "0.0.0.0")]
        public string OuterIP { get; set; } 
        
        [Option("OuterPort", Required = false, Default = 0)]
        public int OuterPort { get; set; } 
        
        [Option("LogLevel", Required = false, Default = 0)]
        public int LogLevel { get; set; }
        
        [Option("Console", Required = false, Default = 0)]
        public int Console { get; set; }
        
        [Option("SingleThread", Required = false, Default = 0)]
        public int SingleThread { get; set; }
        
        public Address InnerAddress
        {
            get
            {
                return new Address(this.InnerIP, this.InnerPort);
            }
            set
            {
                this.InnerIP = value.IP;
                this.InnerPort = value.Port;
            }
        }

        public void Awake()
        {
        }
    }
}