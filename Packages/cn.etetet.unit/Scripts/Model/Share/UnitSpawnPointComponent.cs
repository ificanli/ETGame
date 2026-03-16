using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 记录单位运行时出生点信息。
    /// 用于替代配置表中的初始坐标，供巡逻、归位等逻辑读取。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class UnitSpawnPointComponent : Entity, IAwake<float3, quaternion>, IDestroy
    {
        public float3 Position;

        public quaternion Rotation;
    }
}
