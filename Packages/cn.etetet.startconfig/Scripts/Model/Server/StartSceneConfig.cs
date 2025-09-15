using System.Collections.Generic;
using System.ComponentModel;
using System.Net;

namespace ET.Server
{
    public partial class StartSceneConfigCategory
    {
        private readonly MultiMap<int, StartSceneConfig> processScenes = new();

        private readonly Dictionary<long, Dictionary<string, StartSceneConfig>> zoneScenesByName = new();

        private readonly Dictionary<long, MultiMap<int, StartSceneConfig>> zoneSceneByType = new();
        
        private readonly MultiMap<int, StartSceneConfig> sceneByType = new();
        
        public List<StartSceneConfig> GetByProcess(int process)
        {
            return this.processScenes[process];
        }
        
        public StartSceneConfig GetBySceneName(int zone, string name)
        {
            return this.zoneScenesByName[zone][name];
        }
        
        public List<StartSceneConfig> GetBySceneType(int zone, int type)
        {
            return this.zoneSceneByType[zone][type];
        }
        
        public List<StartSceneConfig> GetBySceneType(int type)
        {
            return this.sceneByType[type];
        }
        
        public StartSceneConfig GetOneBySceneType(int zone, int type)
        {
            return this.zoneSceneByType[zone][type][0];
        }

        public override void EndInit()
        {
            foreach (StartSceneConfig startSceneConfig in this.GetAll().Values)
            {
                sceneByType.Add(startSceneConfig.Type, startSceneConfig);
                
                this.processScenes.Add(startSceneConfig.Process, startSceneConfig);
                
                if (!this.zoneScenesByName.ContainsKey(startSceneConfig.Zone))
                {
                    this.zoneScenesByName.Add(startSceneConfig.Zone, new Dictionary<string, StartSceneConfig>());
                }
                this.zoneScenesByName[startSceneConfig.Zone].Add(startSceneConfig.Name, startSceneConfig);
                
                if (!this.zoneSceneByType.ContainsKey(startSceneConfig.Zone))
                {
                    this.zoneSceneByType.Add(startSceneConfig.Zone, new MultiMap<int, StartSceneConfig>());
                }
                this.zoneSceneByType[startSceneConfig.Zone].Add(startSceneConfig.Type, startSceneConfig);
            }
        }
    }
    
    public partial class StartSceneConfig
    {
        public int Type
        {
            get
            {
                return SceneTypeSingleton.Instance.GetSceneType(this.SceneType);
            }
        }

        public ActorId ActorId 
        {
            get
            {
                return new ActorId(this.StartProcessConfig.Address, new FiberInstanceId(this.Id, 1));
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

        // 内网地址外网端口，通过防火墙映射端口过来
        private IPEndPoint innerIPPort;

        public IPEndPoint InnerIPPort
        {
            get
            {
                if (innerIPPort == null)
                {
                    this.innerIPPort = NetworkHelper.ToIPEndPoint($"{this.StartProcessConfig.InnerIP}:{this.Port}");
                }

                return this.innerIPPort;
            }
        }


        public override void EndInit()
        {
        }
    }
}