using Sirenix.OdinInspector;

namespace ET
{
    [LabelText("添加技能效果 (客户端)")]
    [HideReferenceObjectPicker]
    public class EffectClientSpellAdd: EffectNode
    {
        [BTOutput(typeof(Spell))]
        [ReadOnly]
        [BoxGroup("输出参数")]
        public string Spell = BTEvnKey.Spell;
    }
}