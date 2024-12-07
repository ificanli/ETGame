using Sirenix.OdinInspector;

namespace ET
{
    [LabelText("移除技能效果 (服务器)")]
    [HideReferenceObjectPicker]
    public class EffectServerSpellRemove: EffectNode
    {
        [BTOutput(typeof(Spell))]
        [ReadOnly]
        [BoxGroup("输出参数")]
        public string Spell = BTEvnKey.Spell;
    }
}