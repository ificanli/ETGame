namespace ET
{
    public class BTBuffRemoveTypeCase: BTCondition
    {
        [BTInput(typeof(Buff))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Buff;
        
        public BuffFlags BuffRemoveType;
    }
}