using System.Collections.Generic;

namespace ET.Server
{
    [EnableClass]
    public class RogueWeaponModifierSourceData
    {
        public int SlotIndex;
        public Dictionary<int, int> Modifiers { get; set; } = new();
    }

    /// <summary>
    /// 肉鸽枪械改造组件。按来源记录武器修正，支持全局修正和按槽位修正。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class RogueWeaponModifierComponent : Entity, IAwake, IDestroy
    {
        public Dictionary<long, RogueWeaponModifierSourceData> Sources { get; set; } = new();
    }
}
