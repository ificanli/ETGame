namespace ET
{
    public class BTHasThreat: BTCondition
    {
        [BTInput(typeof(Unit))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Unit;
    }
}
