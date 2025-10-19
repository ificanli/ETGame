using System.Collections.Generic;

namespace ET.Server
{
    [ChildOf(typeof(LocationOneType))]
    public class LockInfo: Entity, IAwake<ActorId, EntityRef<CoroutineLock>>, IDestroy
    {
        public ActorId LockActorId;

        public EntityRef<CoroutineLock> CoroutineLock;
    }

    [ChildOf(typeof(LocationManagerComponent))]
    public class LocationOneType: Entity, IAwake
    {
        public readonly Dictionary<long, ActorId> locations = new();

        public readonly Dictionary<long, EntityRef<LockInfo>> lockInfos = new();
    }

    [ComponentOf(typeof(Scene))]
    public class LocationManagerComponent: Entity, IAwake
    {
    }
}