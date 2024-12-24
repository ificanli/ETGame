namespace ET
{
    public class BTCreateBuffUnit: BTAction
    {
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Caster;
        
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Target;
        
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Buff))]
        public string Buff;
        
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTInput(typeof(Unit))]
        public string Unit;
        
        public int UnitConfigId;
    }
}