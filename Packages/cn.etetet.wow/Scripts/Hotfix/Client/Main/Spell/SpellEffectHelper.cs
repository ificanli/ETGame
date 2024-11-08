namespace ET
{
    public static partial class SpellEffectHelper
    {
        public static void RunEffects(Spell spell, int effectTimeType)
        {
            SpellConfig spellConfig = spell.GetConfig();
            if (!spellConfig.EffectsMap.TryGetValue(effectTimeType, out var effects))
            {
                return;
            }

            foreach (int effect in effects)
            {
                EffectConfig effectConfig = EffectConfigCategory.Instance.Get(effect);
                EventSystem.Instance.Invoke(effect,  new Effect(effectConfig, spell, effectTimeType));
            }
        }
    }
}