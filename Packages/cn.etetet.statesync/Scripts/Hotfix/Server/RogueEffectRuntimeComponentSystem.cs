using System.Collections.Generic;
using System.Linq;

namespace ET.Server
{
    [EntitySystemOf(typeof(RogueEffectRuntimeComponent))]
    public static partial class RogueEffectRuntimeComponentSystem
    {
        [EntitySystem]
        private static void Awake(this RogueEffectRuntimeComponent self)
        {
        }

        public static RogueEffectRuntime AddEffectRuntime(this RogueEffectRuntimeComponent self, int optionId, int effectGroupId, RogueEffectEntryConfig effectEntry, long appliedBuffId)
        {
            RogueEffectRuntime runtime = self.AddChild<RogueEffectRuntime>();
            runtime.OptionId = optionId;
            runtime.EffectGroupId = effectGroupId;
            runtime.ExecuteType = effectEntry?.ExecuteType ?? 0;
            runtime.RefId = effectEntry?.RefId ?? 0;
            runtime.Value1 = effectEntry?.Value1 ?? 0;
            runtime.Value2 = effectEntry?.Value2 ?? 0;
            runtime.EffectBuffConfigId = effectEntry?.RefId ?? 0;
            runtime.AppliedBuffId = appliedBuffId;
            return runtime;
        }

        public static RogueEffectRuntime AddLegacyBuffEffect(this RogueEffectRuntimeComponent self, int optionId, int effectGroupId, int buffConfigId, long appliedBuffId)
        {
            return self.AddEffectRuntime(optionId, effectGroupId, new RogueEffectEntryConfig
            {
                ExecuteType = RogueEffectExecuteType.AddBuff,
                RefId = buffConfigId,
            }, appliedBuffId);
        }

        public static int GetTotalValue1ByExecuteType(this RogueEffectRuntimeComponent self, int executeType)
        {
            if (self == null || executeType <= 0)
            {
                return 0;
            }

            int totalValue = 0;
            foreach (Entity entity in self.Children.Values)
            {
                RogueEffectRuntime runtime = entity as RogueEffectRuntime;
                if (runtime == null || runtime.ExecuteType != executeType || runtime.Value1 == 0)
                {
                    continue;
                }

                totalValue += runtime.Value1;
            }

            return totalValue;
        }

        public static void ClearRuntimes(this RogueEffectRuntimeComponent self)
        {
            foreach (long runtimeId in self.Children.Keys.ToArray())
            {
                self.RemoveChild(runtimeId);
            }
        }

        public static void CleanupAllRuntimeSideEffects(this RogueEffectRuntimeComponent self)
        {
            foreach (Entity entity in self.Children.Values)
            {
                RogueEffectRuntime runtime = entity as RogueEffectRuntime;
                if (runtime != null)
                {
                    CleanupRuntimeSideEffects(self, runtime);
                }
            }
        }

        public static int RemoveOneRuntimeByOptionId(this RogueEffectRuntimeComponent self, int optionId, BuffComponent buffComponent, HashSet<long> removedBuffIds)
        {
            // 找到最新的 runtime 确定其 effectGroupId
            long newestRuntimeId = 0;
            int batchEffectGroupId = 0;
            foreach (long runtimeId in self.Children.Keys)
            {
                RogueEffectRuntime runtime = self.GetChild<RogueEffectRuntime>(runtimeId);
                if (runtime == null || runtime.OptionId != optionId)
                {
                    continue;
                }

                if (newestRuntimeId == 0 || runtimeId > newestRuntimeId)
                {
                    newestRuntimeId = runtimeId;
                    batchEffectGroupId = runtime.EffectGroupId;
                }
            }

            if (newestRuntimeId == 0)
            {
                return 0;
            }

            // 查询 EffectGroup 的 Entry 数量，确定一次选择创建了多少个 runtime
            int batchSize = 1;
            RogueRuntimeConfigCategory configCategory = RogueRuntimeConfigCategory.Instance;
            if (configCategory != null && batchEffectGroupId > 0)
            {
                if (configCategory.TryGetEffectGroup(batchEffectGroupId, out RogueEffectGroupConfig groupConfig) && groupConfig?.Entries != null)
                {
                    batchSize = groupConfig.Entries.Count;
                    if (batchSize <= 0) batchSize = 1;
                }
            }

            // 收集同 optionId + effectGroupId 的所有 runtime，按 ID 降序排列，取最新的 batchSize 个
            List<long> candidateIds = new();
            foreach (long runtimeId in self.Children.Keys)
            {
                RogueEffectRuntime runtime = self.GetChild<RogueEffectRuntime>(runtimeId);
                if (runtime == null || runtime.OptionId != optionId || runtime.EffectGroupId != batchEffectGroupId)
                {
                    continue;
                }

                candidateIds.Add(runtimeId);
            }

            // 按 ID 降序排列，取最新的 batchSize 个
            candidateIds.Sort((a, b) => b.CompareTo(a));
            int removeLimit = System.Math.Min(batchSize, candidateIds.Count);

            int removedCount = 0;
            for (int i = 0; i < removeLimit; i++)
            {
                long runtimeId = candidateIds[i];
                RogueEffectRuntime runtime = self.GetChild<RogueEffectRuntime>(runtimeId);
                if (runtime == null)
                {
                    continue;
                }

                if (runtime.AppliedBuffId > 0 && buffComponent != null)
                {
                    Buff buff = buffComponent.GetChild<Buff>(runtime.AppliedBuffId);
                    if (buff != null)
                    {
                        BuffHelper.RemoveBuff(buff, BuffFlags.NoDurationRemove);
                        removedBuffIds?.Add(runtime.AppliedBuffId);
                    }
                }

                CleanupRuntimeSideEffects(self, runtime);
                self.RemoveChild(runtimeId);
                ++removedCount;
            }

            return removedCount;
        }

        public static int RemoveRuntimesByEffectGroupId(this RogueEffectRuntimeComponent self, int effectGroupId, BuffComponent buffComponent, HashSet<long> removedBuffIds)
        {
            if (effectGroupId <= 0)
            {
                return 0;
            }

            int removedCount = 0;
            foreach (long runtimeId in self.Children.Keys.ToArray())
            {
                RogueEffectRuntime runtime = self.GetChild<RogueEffectRuntime>(runtimeId);
                if (runtime == null || runtime.EffectGroupId != effectGroupId)
                {
                    continue;
                }

                if (runtime.AppliedBuffId > 0 && buffComponent != null)
                {
                    Buff buff = buffComponent.GetChild<Buff>(runtime.AppliedBuffId);
                    if (buff != null)
                    {
                        BuffHelper.RemoveBuff(buff, BuffFlags.NoDurationRemove);
                        removedBuffIds?.Add(runtime.AppliedBuffId);
                    }
                }

                CleanupRuntimeSideEffects(self, runtime);
                self.RemoveChild(runtimeId);
                ++removedCount;
            }

            return removedCount;
        }

        /// <summary>
        /// 移除 Runtime 时清理副作用（武器改造、体型等）。
        /// </summary>
        private static void CleanupRuntimeSideEffects(RogueEffectRuntimeComponent self, RogueEffectRuntime runtime)
        {
            if (runtime == null) return;
            Unit unit = self.GetParent<Unit>();
            if (unit == null || unit.IsDisposed) return;

            switch (runtime.ExecuteType)
            {
                case RogueEffectExecuteType.WeaponModifier:
                {
                    RogueWeaponModifierComponent weaponMod = unit.GetComponent<RogueWeaponModifierComponent>();
                    weaponMod?.RemoveModifier(runtime.RefId, runtime.Value1);
                    WeaponRuntimeStatsHelper.RefreshUnitWeaponRuntimeStats(unit);
                    break;
                }
                case RogueEffectExecuteType.ScaleModifier:
                {
                    if (runtime.AppliedHpDelta != 0)
                    {
                        NumericComponent numeric = unit.NumericComponent;
                        if (numeric != null)
                        {
                            long maxHpAdd = numeric.GetAsLong(NumericType.MaxHPFinalAdd);
                            numeric.Set(NumericType.MaxHPFinalAdd, maxHpAdd - runtime.AppliedHpDelta);
                        }
                    }
                    break;
                }
            }
        }
    }
}
