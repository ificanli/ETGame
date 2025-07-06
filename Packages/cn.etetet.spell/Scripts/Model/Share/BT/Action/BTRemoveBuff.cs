namespace ET
{
    [Module(ModuleName.Spell)]
    public class BTRemoveBuff: BTAction
    {
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Unit;
        
        public int ConfigId;

        public BuffFlags RemoveType;
    }
}