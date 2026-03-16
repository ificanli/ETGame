using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(RogueWeaponModifierComponent))]
    public static partial class RogueWeaponModifierComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RogueWeaponModifierComponent self)
        {
            self.Modifiers.Clear();
        }

        [EntitySystem]
        private static void Destroy(this RogueWeaponModifierComponent self)
        {
            self.Modifiers.Clear();
        }

        public static void AddModifier(this RogueWeaponModifierComponent self, int modType, int valuePermille)
        {
            if (self.Modifiers.TryGetValue(modType, out int current))
            {
                self.Modifiers[modType] = current + valuePermille;
            }
            else
            {
                self.Modifiers[modType] = valuePermille;
            }
        }

        public static void RemoveModifier(this RogueWeaponModifierComponent self, int modType, int valuePermille)
        {
            if (!self.Modifiers.TryGetValue(modType, out int current)) return;
            int newValue = current - valuePermille;
            if (newValue == 0)
            {
                self.Modifiers.Remove(modType);
            }
            else
            {
                self.Modifiers[modType] = newValue;
            }
        }

        /// <summary>
        /// 获取指定改造类型的总千分比修正。
        /// </summary>
        public static int GetModifier(this RogueWeaponModifierComponent self, int modType)
        {
            return self.Modifiers.TryGetValue(modType, out int value) ? value : 0;
        }

        /// <summary>
        /// 应用修正到基础值：base * (1000 + modifier) / 1000
        /// </summary>
        public static long ApplyModifier(this RogueWeaponModifierComponent self, int modType, long baseValue)
        {
            int modifier = self.GetModifier(modType);
            if (modifier == 0) return baseValue;
            return baseValue * (1000 + modifier) / 1000;
        }
    }
}
