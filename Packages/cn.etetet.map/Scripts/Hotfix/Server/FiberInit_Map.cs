using System.Collections.Generic;
using System.Net;

namespace ET.Server
{
    [Invoke(SceneType.Map)]
    public class FiberInit_Map: AInvokeHandler<FiberInit, ETTask>
    {
        public override async ETTask Handle(FiberInit fiberInit)
        {
            Scene root = fiberInit.Fiber.Root;
            EntityRef<Scene> rootRef = root;
            
            root.AddComponent<MailBoxComponent, int>(MailBoxType.UnOrderedMessage);
            root.AddComponent<TimerComponent>();
            root.AddComponent<CoroutineLockComponent>();
            root.AddComponent<ProcessInnerSender>();
            root.AddComponent<MessageSender>();
            UnitComponent unitComponent = root.AddComponent<UnitComponent>();
            
            root.AddComponent<AOIManagerComponent>();
            root.AddComponent<LocationProxyComponent>();
            root.AddComponent<MessageLocationSenderComponent>();
            
            EntityRef<UnitComponent> unitComponentRef = unitComponent;

            string mapName = root.Name.GetSceneConfigName();

            if (mapName != "GateMap")
            {
                // 加载场景寻路数据
                await NavmeshComponent.Instance.Load(mapName);
            }

            root = rootRef;
            unitComponent = unitComponentRef;

            // 加载 ECA 点配置
            ECALoader.LoadFromFile(root, mapName);

            foreach (var kv in MapUnitConfigCategory.Instance.GetAll())
            {
                if (mapName != kv.Value.MapName)
                {
                    continue;
                }
                Unit unit = UnitFactory.Create(root, kv.Key, kv.Value.UnitConfigId);
                unitComponent.Add(unit);
            }
            
            ServiceDiscoveryProxy serviceDiscoveryProxy = root.AddComponent<ServiceDiscoveryProxy>();
            EntityRef<ServiceDiscoveryProxy> serviceDiscoveryProxyComponentRef = serviceDiscoveryProxy;
            await serviceDiscoveryProxy.RegisterToServiceDiscovery();
            // 订阅location,并未注册Map
            serviceDiscoveryProxy = serviceDiscoveryProxyComponentRef;
            await serviceDiscoveryProxy.SubscribeServiceChange("Location", 
                new StringKV()
                {
                    {ServiceMetaKey.SceneType, SceneTypeSingleton.Instance.GetSceneName(SceneType.Location)},
                });
            
            serviceDiscoveryProxy = serviceDiscoveryProxyComponentRef;
            await serviceDiscoveryProxy.SubscribeServiceChange("MapManager", 
                new StringKV()
                {
                    {ServiceMetaKey.SceneType, SceneTypeSingleton.Instance.GetSceneName(SceneType.MapManager)},
                });
        }
    }
}