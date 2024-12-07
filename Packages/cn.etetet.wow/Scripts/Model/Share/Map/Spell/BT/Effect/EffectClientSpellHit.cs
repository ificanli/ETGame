using Sirenix.OdinInspector;

namespace ET
{
    [LabelText("技能击中特效 (客户端)")]
    [HideReferenceObjectPicker]
    public class EffectClientSpellHit: EffectNode
    {
        [BTOutput(typeof(Spell))]
        [ReadOnly]
        [BoxGroup("输出参数")]
        public string Spell = BTEvnKey.Spell;
    }
}