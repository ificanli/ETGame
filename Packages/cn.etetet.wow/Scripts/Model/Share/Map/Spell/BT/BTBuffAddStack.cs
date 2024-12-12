namespace ET
{
    public class BTBuffAddStack: BTNode
    {
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Buff))]
        public string Buff;
        
        public int Value;
    }
}