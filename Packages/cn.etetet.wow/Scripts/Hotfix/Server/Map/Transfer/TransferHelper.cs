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

        public static async ETTask TransferLock(Unit unit, string mapName, bool isLockUnit)
        {
            if (isLockUnit)
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
            request.OldActorId = unit.GetActorId();
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
            // location加锁
            await root.GetComponent<LocationProxyComponent>().Lock(LocationType.Unit, unitId, request.OldActorId);
            
            root = rootRef;
            // 申请地图副本
            A2MapManager_GetMapRequest mapRequest = A2MapManager_GetMapRequest.Create();
            mapRequest.MapName = mapName;
            StartSceneConfig mapManagerConfig = StartSceneConfigCategory.Instance.GetOneBySceneType(root.Zone(), SceneType.MapManager);
            root = rootRef;
            A2MapManager_GetMapResponse mapResponse = await root.GetComponent<MessageSender>().Call(mapManagerConfig.ActorId, mapRequest) as A2MapManager_GetMapResponse;

            
            root = rootRef;
            await root.GetComponent<MessageSender>().Call(mapResponse.MapActorId, request);
        }
    }
}