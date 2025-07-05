using UnityEngine;

namespace ET.Client
{
    [Module(ModuleName.Spell)]
    public struct BTEvent_ShowCastSlider
    {
        public EntityRef<Unit>   Unit;
        public EntityRef<Buff>   Buff;
        public bool   IsIncrease;
        public string IconName;
        public string ShowDisplayName;
    }
}