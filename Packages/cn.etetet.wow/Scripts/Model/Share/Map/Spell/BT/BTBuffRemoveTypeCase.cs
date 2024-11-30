namespace ET
{
    public class BTBuffRemoveTypeCase : BTNode
    {
        [BTInput(typeof(Buff))]
#if UNITY
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
#endif
        public string Buff;
        
        public BuffRemoveType BuffRemoveType;
    }
}