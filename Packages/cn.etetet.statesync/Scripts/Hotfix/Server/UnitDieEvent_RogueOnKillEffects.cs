namespace ET.Server
{
    /// <summary>
    /// 击杀触发肉鸽效果：回血、叠加攻击力。
    /// </summary>
    [Event(SceneType.Map)]
    public class UnitDieEvent_RogueOnKillEffects : AEvent<Scene, UnitDie>
    {
        protected override async ETTask Run(Scene scene, UnitDie a)
        {
            Unit killer = a.Unit;
            if (killer == null || killer.IsDisposed || killer.UnitType != UnitType.Player)
            {
                await ETTask.CompletedTask;
                return;
            }

            RogueBuffPassiveRuntimeComponent passiveRuntime = killer.GetComponent<RogueBuffPassiveRuntimeComponent>();
            if (passiveRuntime == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            ApplyOnKillHeal(killer, passiveRuntime);
            ApplyOnKillStackAttack(killer, passiveRuntime);

            await ETTask.CompletedTask;
        }

        private static void ApplyOnKillHeal(Unit killer, RogueBuffPassiveRuntimeComponent passiveRuntime)
        {
            NumericComponent numeric = killer.NumericComponent;
            if (numeric == null)
            {
                return;
            }

            long currentHp = numeric.GetAsLong(NumericType.HP);
            long maxHp = numeric.GetAsLong(NumericType.MaxHP);
            if (currentHp >= maxHp || maxHp <= 0)
            {
                return;
            }

            long lostHp = maxHp - currentHp;
            long totalHealAmount = 0;
            foreach (int healLostHpPermille in passiveRuntime.OnKillHealPermilleBySource.Values)
            {
                if (healLostHpPermille <= 0)
                {
                    continue;
                }

                long healAmount = lostHp * healLostHpPermille / 1000;
                if (healAmount <= 0)
                {
                    healAmount = 1;
                }

                totalHealAmount += healAmount;
            }

            if (totalHealAmount <= 0)
            {
                return;
            }

            long newHp = currentHp + totalHealAmount;
            if (newHp > maxHp)
            {
                newHp = maxHp;
            }

            numeric.Set(NumericType.HP, newHp);
            Log.Info($"[RogueOnKill] heal applied, unitId={killer.Id}, heal={totalHealAmount}, hp={currentHp}->{newHp}");
        }

        private static void ApplyOnKillStackAttack(Unit killer, RogueBuffPassiveRuntimeComponent passiveRuntime)
        {
            using ListComponent<long> stackSourceIds = ListComponent<long>.Create();
            foreach (long sourceId in passiveRuntime.OnKillStackBySource.Keys)
            {
                stackSourceIds.Add(sourceId);
            }

            foreach (long sourceId in stackSourceIds)
            {
                if (!passiveRuntime.OnKillStackBySource.TryGetValue(sourceId, out RogueOnKillStackSourceData stackState))
                {
                    continue;
                }

                if (stackState.BonusPermillePerStack <= 0)
                {
                    continue;
                }

                int maxStacks = stackState.MaxStacks > 0 ? stackState.MaxStacks : 1;
                if (stackState.CurrentStacks >= maxStacks)
                {
                    continue;
                }

                stackState.CurrentStacks += 1;
                passiveRuntime.OnKillStackBySource[sourceId] = stackState;
                Log.Info($"[RogueOnKill] stack attack, unitId={killer.Id}, source={sourceId}, stacks={stackState.CurrentStacks}/{maxStacks}, bonusPermille={stackState.BonusPermillePerStack}");
            }
        }
    }
}
