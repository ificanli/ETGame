namespace ET
{
    public class AI_PetZhuiJiCheck: BTCondition
    {
        [BTInput(typeof(Buff))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Buff;
    }
}
