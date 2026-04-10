using System.Collections.Generic;

namespace ET.Server
{
    [EntitySystemOf(typeof(RogueWeaponModifierComponent))]
    public static partial class RogueWeaponModifierComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RogueWeaponModifierComponent self)
        {
            self.Sources.Clear();
        }

        [EntitySystem]
        private static void Destroy(this RogueWeaponModifierComponent self)
        {
            self.Sources.Clear();
        }

        public static void SetSource(this RogueWeaponModifierComponent self, long sourceId, int slotIndex, List<RogueWeaponModifierEntry> entries)
        {
            if (self == null || self.IsDisposed || sourceId == 0)
            {
                return;
            }

            RogueWeaponModifierSourceData sourceData = new RogueWeaponModifierSourceData
            {
                SlotIndex = NormalizeSlotIndex(slotIndex),
            };

            if (entries != null)
            {
                foreach (RogueWeaponModifierEntry entry in entries)
                {
                    if (entry == null || entry.ModType <= 0 || entry.ValuePermille == 0)
                    {
                        continue;
                    }

                    sourceData.Modifiers[entry.ModType] = entry.ValuePermille;
                }
            }

            self.Sources[sourceId] = sourceData;
        }

        public static void AddOrAccumulateModifier(this RogueWeaponModifierComponent self, long sourceId, int slotIndex, int modType, int value)
        {
            if (self == null || self.IsDisposed || sourceId == 0 || modType <= 0 || value == 0)
            {
                return;
            }

            if (!self.Sources.TryGetValue(sourceId, out RogueWeaponModifierSourceData sourceData) || sourceData == null)
            {
                sourceData = new RogueWeaponModifierSourceData();
            }

            sourceData.SlotIndex = NormalizeSlotIndex(slotIndex);
            if (sourceData.Modifiers.TryGetValue(modType, out int current))
            {
                sourceData.Modifiers[modType] = current + value;
            }
            else
            {
                sourceData.Modifiers[modType] = value;
            }

            self.Sources[sourceId] = sourceData;
        }

        public static void RemoveSource(this RogueWeaponModifierComponent self, long sourceId)
        {
            if (self == null || self.IsDisposed || sourceId == 0)
            {
                return;
            }

            self.Sources.Remove(sourceId);
        }

        public static int GetModifier(this RogueWeaponModifierComponent self, int slotIndex, int modType)
        {
            if (self == null || self.IsDisposed || modType <= 0)
            {
                return 0;
            }

            int normalizedSlotIndex = NormalizeSlotIndex(slotIndex);
            int total = 0;
            foreach (RogueWeaponModifierSourceData sourceData in self.Sources.Values)
            {
                if (sourceData == null ||
                    sourceData.Modifiers == null ||
                    sourceData.Modifiers.Count == 0 ||
                    (sourceData.SlotIndex != 0 && sourceData.SlotIndex != normalizedSlotIndex))
                {
                    continue;
                }

                if (sourceData.Modifiers.TryGetValue(modType, out int value) && value != 0)
                {
                    total += value;
                }
            }

            return total;
        }

        public static bool IsEmpty(this RogueWeaponModifierComponent self)
        {
            return self == null || self.IsDisposed || self.Sources.Count == 0;
        }

        private static int NormalizeSlotIndex(int slotIndex)
        {
            return slotIndex == 1 || slotIndex == 2 ? slotIndex : 0;
        }
    }
}
