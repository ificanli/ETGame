using System.Collections.Generic;

namespace ET.Server
{
    /// <summary>
    /// 肉鸽枪械改造组件。存储各类武器属性修正值（千分比）。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class RogueWeaponModifierComponent : Entity, IAwake, IDestroy
    {
        /// <summary>
        /// Key = WeaponModType, Value = 累计千分比修正值
        /// </summary>
        public Dictionary<int, int> Modifiers { get; set; } = new();
    }
}
