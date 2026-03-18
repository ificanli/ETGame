namespace ET.Server
{
    [Event(SceneType.Map)]
    public class RogueAfterDamageApply_HitHeroCritGrowth : AEvent<Scene, RogueAfterDamageApply>
    {
        protected override async ETTask Run(Scene scene, RogueAfterDamageApply args)
        {
            RogueHitHeroCritGrowthHelper.TryApply(args.Context, scene?.GetComponent<UnitComponent>());
            await ETTask.CompletedTask;
        }
    }

    public static class RogueHitHeroCritGrowthHelper
    {
        public static bool TryApply(DamageContext ctx, UnitComponent unitComponent)
        {
            if (ctx == null || !ctx.IsBullet || ctx.SourceUnitId == 0 || ctx.TargetUnitId == 0 || unitComponent == null)
            {
                return false;
            }

            Unit attacker = unitComponent.Get(ctx.SourceUnitId);
            Unit target = unitComponent.Get(ctx.TargetUnitId);
            RogueHitHeroCritStateComponent stateComponent = attacker?.GetComponent<RogueHitHeroCritStateComponent>();
            if (attacker == null ||
                attacker.IsDisposed ||
                attacker.UnitType != UnitType.Player ||
                target == null ||
                target.IsDisposed ||
                target.UnitType != UnitType.Player ||
                stateComponent == null ||
                !CampHelper.IsEnemy(attacker, target))
            {
                return false;
            }

            stateComponent.OnHitHero();
            return true;
        }
    }
}
