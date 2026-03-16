namespace ET
{
    /// <summary>
    /// 创建眼单位的 BT 节点。
    /// </summary>
    public class BTCreateWard : BTAction
    {
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Caster;

        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Buff))]
        public string Buff;

        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unity.Mathematics.float3))]
        public string TargetPos;

        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Unit;

        public int UnitConfigId;

        public float VisionRadius;
    }
}
