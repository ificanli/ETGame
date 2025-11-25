using System.Collections.Generic;
using System.Linq;
using MongoDB.Bson;

namespace ET.Server
{
    public static partial class TransferHelper
    {
        public static async ETTask TransferAtFrameFinish(Unit unit, string mapName, int mapId)
        {
            EntityRef<Unit> unitRef = unit;
            await unit.Fiber().WaitFrameFinish();
            unit = unitRef;
            await TransferHelper.TransferLock(unit, mapName, mapId, true);
        }

        // needLock 是否需要加上Mailbox协程锁
        public static async ETTask TransferLock(Unit unit, string mapName, long mapId, bool needLock)
        {
            if (needLock)
            {
                EntityRef<Unit> unitRef = unit;
                using var _ = await unit.Root().GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.Mailbox, unit.Id);
                unit = unitRef;
                await TransferHelper.Transfer(unit, mapName, mapId);
            }
            else
            {
                await TransferHelper.Transfer(unit, mapName, mapId);
            }
        }

        private static async ETTask Transfer(Unit unit, string mapName, long mapId)
        {
            Scene root = unit.Root();
            EntityRef<Scene> rootRef = root;
            EntityRef<Unit> unitRef = unit;
            long unitId = unit.Id;
            
            bool changeScene = TransferSceneHelper.IsChangeScene(unit.Scene().Name, mapName);
            
            
            //1. 申请地图副本
            A2MapManager_GetMapRequest mapRequest = A2MapManager_GetMapRequest.Create();
            mapRequest.MapName = mapName;
            mapRequest.UnitId = unitId;
            mapRequest.MapId = mapId;
            ServiceDiscoveryProxy serviceDiscoveryProxy = root.GetComponent<ServiceDiscoveryProxy>();
            EntityRef<ServiceDiscoveryProxy> serviceDiscoveryProxyRef = serviceDiscoveryProxy;
            string mapManagerName = serviceDiscoveryProxy.GetBySceneTypeAndZone(SceneType.MapManager, root.Zone())[0].SceneName;
            A2MapManager_GetMapResponse mapResponse = await serviceDiscoveryProxy.Call(mapManagerName, mapRequest) as A2MapManager_GetMapResponse;
            
            //2. 传送
            unit = unitRef;
            await Transfer(unit, mapResponse.MapActorId, changeScene);


            //3. 通知副本，玩家已经传送进入副本
            A2MapManager_NotifyPlayerAlreadyEnterMapRequest a2MapManagerNotifyPlayerAlreadyEnterMapRequest = A2MapManager_NotifyPlayerAlreadyEnterMapRequest.Create();
            a2MapManagerNotifyPlayerAlreadyEnterMapRequest.MapName = mapResponse.MapName;
            a2MapManagerNotifyPlayerAlreadyEnterMapRequest.UnitId = unitId;
            a2MapManagerNotifyPlayerAlreadyEnterMapRequest.MapId = mapResponse.MapId;
            
            serviceDiscoveryProxy = serviceDiscoveryProxyRef;
            await serviceDiscoveryProxy.Call(mapManagerName, a2MapManagerNotifyPlayerAlreadyEnterMapRequest);
        }

        private static async ETTask Transfer(Unit unit, ActorId mapActorId, bool changeScene)
        {
            EntityRef<Unit> unitRef = unit;
            long unitId = unit.Id;
            Log.Debug("start transfer1 unit: " + unitId + ", mapActorId: " + mapActorId + ", changeScene: " + changeScene);
            
            Scene root = unit.Root();
            EntityRef<Scene> rootRef = root;
            
            ActorId oldActorId = unit.GetActorId();
            //2. location加锁
            await root.GetComponent<LocationProxyComponent>().Lock(LocationType.Unit, unitId, oldActorId);
            
            // 先从AOI中移除
            unit = unitRef;
            unit.RemoveComponent<AOIEntity>();

            ActorId newActorId = default;
            M2M_UnitTransferRequest request = M2M_UnitTransferRequest.Create();
            request.OldActorId = oldActorId;
            request.ChangeScene = changeScene;
            
            // 同一个进程
            if (mapActorId.Address == AddressSingleton.Instance.InnerAddress)
            {
                foreach (Entity entity in unit.Components.Values.ToArray())
                {
                    if (entity is not ITransfer)
                    {
                        unit.RemoveComponent(entity.GetType());
                    }
                }
                
                request.Unit = unit;
                
                // 这里需要移除Unit，但是不能Dispose，里面会把Unit部分数据Reset
                unit.GetParent<UnitComponent>().Remove(unit.Id, false);
            }
            else // 不同进程
            {
                //3. 拼装消息
                request.UnitBytes = unit.ToBson();
                foreach (Entity entity in unit.Components.Values)
                {
                    if (entity is ITransfer)
                    {
                        request.EntityBytes.Add(entity.ToBson());
                    }
                }
                unit.GetParent<UnitComponent>().Remove(unit.Id);
            }

            Log.Debug("start transfer2 unit: " + unitId + ", mapActorId: " + mapActorId + ", changeScene: " + changeScene);
            //4. 传送到副本
            root = rootRef;
            M2M_UnitTransferResponse response = await root.GetComponent<MessageSender>().Call(mapActorId, request) as M2M_UnitTransferResponse;
            newActorId = response.NewActorId;
            Log.Debug("start transfer3 unit: " + unitId + ", mapActorId: " + mapActorId + ", changeScene: " + changeScene);

            root = rootRef;
            //5. 解锁location，可以接收发给Unit的消息
            await root.GetComponent<LocationProxyComponent>().UnLock(LocationType.Unit, unitId, oldActorId, newActorId);
            
            Log.Debug("start transfer4 unit: " + unitId + ", mapActorId: " + mapActorId + ", changeScene: " + changeScene);
        }
    }
}