using System.Collections.Generic;
using MongoDB.Bson;

namespace ET.Server
{
    public static partial class TransferHelper
    {
        public static async ETTask TransferAtFrameFinish(Unit unit, string mapName)
        {
            EntityRef<Unit> unitRef = unit;
            await unit.Fiber().WaitFrameFinish();
            unit = unitRef;
            await TransferHelper.TransferLock(unit, mapName, true);
        }

        // needLock 是否需要加上Mailbox协程锁
        public static async ETTask TransferLock(Unit unit, string mapName, bool needLock)
        {
            if (needLock)
            {
                EntityRef<Unit> unitRef = unit;
                using CoroutineLock _ = await unit.Root().GetComponent<CoroutineLockComponent>().Wait(CoroutineLockType.Mailbox, unit.Id);
                unit = unitRef;
                await TransferHelper.Transfer(unit, mapName);
            }
            else
            {
                await TransferHelper.Transfer(unit, mapName);
            }
        }

        private static async ETTask Transfer(Unit unit, string mapName)
        {
            Scene root = unit.Root();
            EntityRef<Scene> rootRef = root;
            long unitId = unit.Id;
            
            M2M_UnitTransferRequest request = M2M_UnitTransferRequest.Create();
            ActorId oldActorId = unit.GetActorId();
            request.OldActorId = oldActorId;
            request.Unit = unit.ToBson();
            foreach (Entity entity in unit.Components.Values)
            {
                if (entity is ITransfer)
                {
                    request.Entitys.Add(entity.ToBson());
                }
            }
            unit.Dispose();
            
            root = rootRef;
            //1. location加锁
            await root.GetComponent<LocationProxyComponent>().Lock(LocationType.Unit, unitId, oldActorId);
            
            root = rootRef;
            //2. 申请地图副本
            A2MapManager_GetMapRequest mapRequest = A2MapManager_GetMapRequest.Create();
            mapRequest.MapName = mapName;
            mapRequest.UnitId = unitId;
            StartSceneConfig mapManagerConfig = StartSceneConfigCategory.Instance.GetOneBySceneType(root.Zone(), SceneType.MapManager);
            root = rootRef;
            A2MapManager_GetMapResponse mapResponse = await root.GetComponent<MessageSender>().Call(mapManagerConfig.ActorId, mapRequest) as A2MapManager_GetMapResponse;

            
            //3. 传送到副本
            root = rootRef;
            M2M_UnitTransferResponse response = await root.GetComponent<MessageSender>().Call(mapResponse.MapActorId, request) as M2M_UnitTransferResponse;
            
            //4. 通知副本，玩家已经传送进入副本
            A2MapManager_NotifyPlayerAlreadyEnterMapRequest a2MapManagerNotifyPlayerAlreadyEnterMapRequest = A2MapManager_NotifyPlayerAlreadyEnterMapRequest.Create();
            a2MapManagerNotifyPlayerAlreadyEnterMapRequest.MapName = mapName;
            a2MapManagerNotifyPlayerAlreadyEnterMapRequest.UnitId = unitId;
            root = rootRef;
            await root.GetComponent<MessageSender>().Call(mapManagerConfig.ActorId, a2MapManagerNotifyPlayerAlreadyEnterMapRequest);
            
            
            root = rootRef;
            //5. 解锁location，可以接收发给Unit的消息
            await root.GetComponent<LocationProxyComponent>().UnLock(LocationType.Unit, unitId, oldActorId, response.NewActorId);
        }
    }
}