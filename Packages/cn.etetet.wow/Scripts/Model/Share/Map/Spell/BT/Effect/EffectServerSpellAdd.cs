using Sirenix.OdinInspector;

namespace ET
{
    [LabelText("添加技能效果 (服务器)")]
    [HideReferenceObjectPicker]
    public class EffectServerSpellAdd: EffectNode
    {
        [BTOutput(typeof(Spell))]
        [ReadOnly]
        [BoxGroup("输出参数")]
        public string Spell = BTEvnKey.Spell;
    }
}