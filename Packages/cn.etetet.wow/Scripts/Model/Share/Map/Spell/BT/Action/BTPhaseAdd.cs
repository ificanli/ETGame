namespace ET.Server
{
    public class BTPhaseAdd: BTAction
    {
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        [BTInput(typeof(Unit))]
        public string Unit;
        
        public PhaseType phaseType;
    }
}