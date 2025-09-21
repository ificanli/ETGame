using System.Collections.Generic;
using System.Net;

namespace ET.Server
{
    public partial class StartSceneConfigCategory
    {
        private readonly MultiMap<int, StartSceneConfig> processScenes = new();
        
        private readonly Dictionary<string, StartSceneConfig> sceneNameScenes = new();
        
        public List<StartSceneConfig> GetByProcess(int process)
        {
            return this.processScenes[process];
        }
        
        public StartSceneConfig GetBySceneName(string sceneName)
        {
            return this.sceneNameScenes[sceneName];
        }
        
        public override void EndInit()
        {
            foreach (StartSceneConfig startSceneConfig in this.GetAll().Values)
            {
                this.processScenes.Add(startSceneConfig.Process, startSceneConfig);
                this.sceneNameScenes.Add(startSceneConfig.Name, startSceneConfig);
            }
        }
    }
    
    public partial class StartSceneConfig
    {
        public Address Address 
        {
            get
            {
                return this.StartProcessConfig.Address;
            }
        }

        public StartProcessConfig StartProcessConfig
        {
            get
            {
                return StartProcessConfigCategory.Instance.Get(this.Process);
            }
        }
        
        public StartZoneConfig StartZoneConfig
        {
            get
            {
                return StartZoneConfigCategory.Instance.Get(this.Zone);
            }
        }

        public IPEndPoint InnerIPOuterPort
        {
            get
            {
                return NetworkHelper.ToIPEndPoint($"{this.StartProcessConfig.InnerIP}:{this.Port}");
            }
        }
        
        public IPEndPoint OuterIPOuterPort
        {
            get
            {
                return NetworkHelper.ToIPEndPoint($"{this.StartProcessConfig.OuterIP}:{this.Port}");
            }
        }

        public override void EndInit()
        {
        }
    }
}