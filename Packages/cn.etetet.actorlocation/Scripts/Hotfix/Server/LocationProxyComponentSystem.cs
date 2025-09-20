using System;

namespace ET.Server
{
    [EntitySystemOf(typeof(LocationProxyComponent))]
    public static partial class LocationProxyComponentSystem
    {
        [EntitySystem]
        private static void Awake(this LocationProxyComponent self)
        {
        }
        
        private static string GetLocationSceneId(this LocationProxyComponent self, long key)
        {
            ServiceDiscoveryProxyComponent serviceDiscoveryProxyComponent = self.Root().GetComponent<ServiceDiscoveryProxyComponent>();
            // 这里默认取第一个location，如果是mmo，就需要根据key获取zone
            string locationName = serviceDiscoveryProxyComponent.GetBySceneType(SceneType.Location)[0];
            return locationName;
        }

        public static async ETTask Add(this LocationProxyComponent self, int type, long key, ActorId actorId)
        {
            Log.Info($"location proxy add {key}, {actorId} {TimeInfo.Instance.ServerNow()}");
            ObjectAddRequest objectAddRequest = ObjectAddRequest.Create();
            objectAddRequest.Type = type;
            objectAddRequest.Key = key;
            objectAddRequest.ActorId = actorId;
            await self.Root().GetComponent<ServiceDiscoveryProxyComponent>().Call(self.GetLocationSceneId(key), objectAddRequest);
        }

        public static async ETTask Lock(this LocationProxyComponent self, int type, long key, ActorId actorId, int time = 60000)
        {
            Log.Info($"location proxy lock {key}, {actorId} {TimeInfo.Instance.ServerNow()}");

            ObjectLockRequest objectLockRequest = ObjectLockRequest.Create();
            objectLockRequest.Type = type;
            objectLockRequest.Key = key;
            objectLockRequest.ActorId = actorId;
            objectLockRequest.Time = time;
            await self.Root().GetComponent<ServiceDiscoveryProxyComponent>().Call(self.GetLocationSceneId(key), objectLockRequest);
        }

        public static async ETTask UnLock(this LocationProxyComponent self, int type, long key, ActorId oldActorId, ActorId newActorId)
        {
            Log.Info($"location proxy unlock {key}, {newActorId} {TimeInfo.Instance.ServerNow()}");
            ObjectUnLockRequest objectUnLockRequest = ObjectUnLockRequest.Create();
            objectUnLockRequest.Type = type;
            objectUnLockRequest.Key = key;
            objectUnLockRequest.OldActorId = oldActorId;
            objectUnLockRequest.NewActorId = newActorId;
            await self.Root().GetComponent<ServiceDiscoveryProxyComponent>().Call(self.GetLocationSceneId(key), objectUnLockRequest);
        }

        public static async ETTask Remove(this LocationProxyComponent self, int type, long key)
        {
            Log.Info($"location proxy remove {key}, {TimeInfo.Instance.ServerNow()}");

            ObjectRemoveRequest objectRemoveRequest = ObjectRemoveRequest.Create();
            objectRemoveRequest.Type = type;
            objectRemoveRequest.Key = key;
            await self.Root().GetComponent<ServiceDiscoveryProxyComponent>().Call(self.GetLocationSceneId(key), objectRemoveRequest);
        }

        public static async ETTask<ActorId> Get(this LocationProxyComponent self, int type, long key)
        {
            if (key == 0)
            {
                throw new Exception($"get location key 0");
            }

            // location server配置到共享区，一个大战区可以配置N多个location server,这里暂时为1
            ObjectGetRequest objectGetRequest = ObjectGetRequest.Create();
            objectGetRequest.Type = type;
            objectGetRequest.Key = key;
            ObjectGetResponse response = (ObjectGetResponse)await self.Root().GetComponent<ServiceDiscoveryProxyComponent>().Call(
                self.GetLocationSceneId(key), objectGetRequest);
            return response.ActorId;
        }

        public static async ETTask AddLocation(this Entity self, int type)
        {
            await self.Root().GetComponent<LocationProxyComponent>().Add(type, self.Id, self.GetActorId());
        }

        public static async ETTask RemoveLocation(this Entity self, int type)
        {
            await self.Root().GetComponent<LocationProxyComponent>().Remove(type, self.Id);
        }
    }
}