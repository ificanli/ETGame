using System.Collections.Generic;
using System.Net;

namespace ET.Server
{
    public partial class StartSceneConfigCategory
    {
        private readonly MultiMap<int, StartSceneConfig> processScenes = new();

        public ActorId ServiceDiscoveryActorId { get; private set; }
        
        public List<StartSceneConfig> GetByProcess(int process)
        {
            return this.processScenes[process];
        }
        
        public override void EndInit()
        {
            foreach (StartSceneConfig startSceneConfig in this.GetAll().Values)
            {
                this.processScenes.Add(startSceneConfig.Process, startSceneConfig);

                if (startSceneConfig.Type == SceneType.ServiceDiscovery)
                {
                    this.ServiceDiscoveryActorId = startSceneConfig.ActorId;
                }
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