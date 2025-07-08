using System.Collections.Generic;
using MongoDB.Bson;

namespace ET.Server
{
    public static partial class TransferHelper
    {
        public static async ETTask TransferAtFrameFinish(Unit unit, string mapName, int line)
        {
            EntityRef<Unit> unitRef = unit;
            await unit.Fiber().WaitFrameFinish();
            unit = unitRef;
            await TransferHelper.TransferLock(unit, mapName, line, true);
        }

        // needLock 是否需要加上Mailbox协程锁
        public static async ETTask TransferLock(Unit unit, string mapName, int line, bool needLock)
        {
            if (needLock)
            {
                EntityRef<Unit> unitRef = unit;
                using CoroutineLock _ = await unit.Root().GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.Mailbox, unit.Id);
                unit = unitRef;
                await TransferHelper.Transfer(unit, mapName, line);
            }
            else
            {
                await TransferHelper.Transfer(unit, mapName, line);
            }
        }

        private static async ETTask Transfer(Unit unit, string mapName, int line)
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
            mapRequest.Line = line;
            StartSceneConfig mapManagerConfig = StartSceneConfigCategory.Instance.GetOneBySceneType(root.Zone(), SceneType.MapManager);
            root = rootRef;
            A2MapManager_GetMapResponse mapResponse = await root.GetComponent<MessageSender>().Call(mapManagerConfig.ActorId, mapRequest) as A2MapManager_GetMapResponse;
            
            //2. 传送
            root = rootRef;
            unit = unitRef;
            await Transfer(unit, mapResponse.MapActorId, changeScene);


            //3. 通知副本，玩家已经传送进入副本
            A2MapManager_NotifyPlayerAlreadyEnterMapRequest a2MapManagerNotifyPlayerAlreadyEnterMapRequest = A2MapManager_NotifyPlayerAlreadyEnterMapRequest.Create();
            a2MapManagerNotifyPlayerAlreadyEnterMapRequest.MapName = mapResponse.MapName;
            a2MapManagerNotifyPlayerAlreadyEnterMapRequest.UnitId = unitId;
            a2MapManagerNotifyPlayerAlreadyEnterMapRequest.Line = mapResponse.Line;
            root = rootRef;
            await root.GetComponent<MessageSender>().Call(mapManagerConfig.ActorId, a2MapManagerNotifyPlayerAlreadyEnterMapRequest);
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
            
            //3. 拼装消息
            M2M_UnitTransferRequest request = M2M_UnitTransferRequest.Create();
            request.OldActorId = oldActorId;
            request.Unit = unit.ToBson();
            request.ChangeScene = changeScene;
            foreach (Entity entity in unit.Components.Values)
            {
                if (entity is ITransfer)
                {
                    request.Entitys.Add(entity.ToBson());
                }
            }
            unit.Dispose();
            
            Log.Debug("start transfer2 unit: " + unitId + ", mapActorId: " + mapActorId + ", changeScene: " + changeScene);
            //4. 传送到副本
            root = rootRef;
            M2M_UnitTransferResponse response = await root.GetComponent<MessageSender>().Call(mapActorId, request) as M2M_UnitTransferResponse;
            
            Log.Debug("start transfer3 unit: " + unitId + ", mapActorId: " + mapActorId + ", changeScene: " + changeScene);
            root = rootRef;
            //5. 解锁location，可以接收发给Unit的消息
            await root.GetComponent<LocationProxyComponent>().UnLock(LocationType.Unit, unitId, oldActorId, response.NewActorId);
            
            Log.Debug("start transfer4 unit: " + unitId + ", mapActorId: " + mapActorId + ", changeScene: " + changeScene);
        }
    }
}