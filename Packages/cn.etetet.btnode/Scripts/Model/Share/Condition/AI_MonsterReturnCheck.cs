namespace ET
{
    public class AI_MonsterReturnCheck: BTCondition
    {
        [BTInput(typeof(Buff))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Buff;

        [Sirenix.OdinInspector.BoxGroup("配置参数")]
        public float ReturnDistance = 30f;
    }
}
