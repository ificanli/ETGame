using Sirenix.OdinInspector;

namespace ET
{
    [LabelText("移除技能效果 (客户端)")]
    [HideReferenceObjectPicker]
    public class EffectClientSpellRemove: EffectNode
    {
        [BTOutput(typeof(Spell))]
        [ReadOnly]
        [BoxGroup("输出参数")]
        public string Spell = BTEvnKey.Spell;
    }
}