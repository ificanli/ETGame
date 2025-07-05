namespace ET.Server
{
    [Module(ModuleName.Spell)]
    public class BTPhaseRemove: BTAction
    {
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Unit;
        
        public PhaseType phaseType;
    }
}