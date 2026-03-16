using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 测试 CombatStateComponent：受到伤害后进入战斗状态。
    /// </summary>
    public class Test_Rogue_CombatState_EnterCombat_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_Rogue_CombatState_EnterCombat_Test));
            Fiber testFiber = scope.TestFiber;
            Scene scene = testFiber.Root;

            UnitComponent unitComponent = scene.GetComponent<UnitComponent>() ?? scene.AddComponent<UnitComponent>();
            if (scene.TimerComponent == null)
            {
                scene.AddComponent<TimerComponent>();
            }

            UnitConfig playerConfig = null;
            foreach (UnitConfig config in UnitConfigCategory.Instance.DataList)
            {
                if (config.UnitType == UnitType.Player)
                {
                    playerConfig = config;
                    break;
                }
            }

            if (playerConfig == null)
            {
                Log.Console("player unit config is null");
                return 1;
            }

            Unit unit = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), playerConfig.Id);
            unit.UnitType = UnitType.Player;
            unit.AddComponent<NumericComponent>().SetNoEvent(NumericType.HP, 1000);

            CombatStateComponent combatState = unit.AddComponent<CombatStateComponent>();

            if (combatState.InCombat)
            {
                Log.Console("should not be in combat initially");
                return 2;
            }

            // 模拟受到伤害
            combatState.OnTakeDamage();

            if (!combatState.InCombat)
            {
                Log.Console("should be in combat after taking damage");
                return 3;
            }

            if (combatState.LastTakeDamageTime <= 0)
            {
                Log.Console($"last take damage time should be set, actual={combatState.LastTakeDamageTime}");
                return 4;
            }

            // 模拟造成伤害
            combatState.OnDealDamage();

            if (combatState.LastDealDamageTime <= 0)
            {
                Log.Console($"last deal damage time should be set, actual={combatState.LastDealDamageTime}");
                return 5;
            }

            return ErrorCode.ERR_Success;
        }
    }
}
