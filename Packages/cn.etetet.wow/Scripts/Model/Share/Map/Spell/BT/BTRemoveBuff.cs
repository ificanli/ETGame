namespace ET
{
    public class BTRemoveBuff: BTNode
    {
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Unit;
        
        public int ConfigId;

        public BuffFlags RemoveType;
    }
}