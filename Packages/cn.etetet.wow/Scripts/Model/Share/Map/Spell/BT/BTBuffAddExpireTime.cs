namespace ET
{
    public class BTBuffAddExpireTime: BTNode
    {
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Buff))]
        public string Buff;
        
        public int Value;

        public int Pct;
    }
}