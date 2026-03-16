namespace ET
{
    /// <summary>
    /// 给 Buff 挂载侦查能力的 BT 节点。
    /// </summary>
    public class BTApplyDetector : BTAction
    {
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Unit;

        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Buff))]
        public string Buff;

        public float RevealRadius;
    }
}
