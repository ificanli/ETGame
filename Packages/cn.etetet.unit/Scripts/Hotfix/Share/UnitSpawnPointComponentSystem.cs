using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 单位出生点组件系统。
    /// </summary>
    [EntitySystemOf(typeof(UnitSpawnPointComponent))]
    public static partial class UnitSpawnPointComponentSystem
    {
        [EntitySystem]
        private static void Awake(this UnitSpawnPointComponent self, float3 position, quaternion rotation)
        {
            self.Position = position;
            self.Rotation = rotation;
        }

        [EntitySystem]
        private static void Destroy(this UnitSpawnPointComponent self)
        {
            self.Position = float3.zero;
            self.Rotation = quaternion.identity;
        }

        public static void SetSpawnPoint(this UnitSpawnPointComponent self, float3 position, quaternion rotation)
        {
            self.Position = position;
            self.Rotation = rotation;
        }
    }
}
