namespace ET.Server
{
    /// <summary>
    /// 致命伤害拦截：当伤害会导致死亡时，检查是否有金币免死效果。
    /// 消耗当前金币的指定百分比来免疫本次致命伤害。
    /// </summary>
    [Event(SceneType.Map)]
    public class RogueBeforeDamageApply_FatalImmunity : AEvent<Scene, RogueBeforeDamageApply>
    {
        protected override async ETTask Run(Scene scene, RogueBeforeDamageApply a)
        {
            DamageContext ctx = a.Context;
            if (ctx == null || ctx.TargetUnitId <= 0)
            {
                await ETTask.CompletedTask;
                return;
            }

            UnitComponent unitComponent = scene?.GetComponent<UnitComponent>();
            Unit target = unitComponent?.Get(ctx.TargetUnitId);
            if (target == null || target.IsDisposed || target.UnitType != UnitType.Player)
            {
                await ETTask.CompletedTask;
                return;
            }

            // 检查伤害是否致命
            NumericComponent numeric = target.NumericComponent;
            if (numeric == null)
            {
                await ETTask.CompletedTask;
                return;
            }

            long currentHp = numeric.GetAsLong(NumericType.HP);
            if (currentHp <= 0 || ctx.FinalDamage < currentHp)
            {
                // 不致命，不需要拦截
                await ETTask.CompletedTask;
                return;
            }

            // 检查是否有金币免死效果（通过 RogueGlobalConfig 的 FatalImmuneGoldCostPct）
            RogueProgressComponent progress = target.GetComponent<RogueProgressComponent>();
            if (progress == null || progress.CurrentGold <= 0)
            {
                await ETTask.CompletedTask;
                return;
            }

            if (!RogueEffectQueryHelper.HasFatalImmunity(target))
            {
                await ETTask.CompletedTask;
                return;
            }

            // 消耗金币，设置 CanDie = false
            int costPct = GetFatalImmunityGoldCostPct();
            int goldCost = (int)((long)progress.CurrentGold * costPct / 100);
            if (goldCost <= 0)
            {
                goldCost = 1;
            }

            RogueGoldHelper.TryCostGold(progress, goldCost);
            ctx.CanDie = false;

            Log.Info($"[RogueFatalImmunity] fatal damage intercepted, unitId={target.Id}, goldCost={goldCost}, remainGold={progress.CurrentGold}");
            RogueProgressHelper.SyncProgress(target, progress);

            await ETTask.CompletedTask;
        }

        /// <summary>
        /// 致命免死消耗金币百分比。
        /// TODO: 等 RogueGlobalConfig.xlsx 加了 FatalImmuneGoldCostPct 字段后从配置读取。
        /// </summary>
        private static int GetFatalImmunityGoldCostPct()
        {
            return 50;
        }
    }
}
