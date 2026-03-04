using System.Collections.Generic;

namespace ET.Client
{
    [ComponentOf(typeof(Scene))]
    public class WeaponVisualConfigComponent : Entity, IAwake, IDestroy
    {
        public Dictionary<int, WeaponVisualConfig> Configs = new();
    }

    public struct WeaponVisualConfig
    {
        public int WeaponId;
        public string ProjectileEffect;
        public string HitEffect;
        public int CasterBindPointId;
        public int TargetBindPointId;
        public float ProjectileSpeed;
        public int ProjectileDurationMs;
        public int HitEffectDurationMs;
    }
}
