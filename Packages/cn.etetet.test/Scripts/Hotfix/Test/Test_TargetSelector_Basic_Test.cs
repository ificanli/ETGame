using System.Reflection;

namespace ET.Test
{
    /// <summary>
    /// 验证 TargetSelectorComponent 默认值，以及手动锁定字段/方法已被删除。
    /// </summary>
    public class Test_TargetSelector_Basic_Test : ATestHandler
    {
        public override async ETTask<int> Handle(TestContext context)
        {
            await using TestFiberScope scope = await TestFiberScope.Create(context.Fiber, nameof(Test_TargetSelector_Basic_Test));
            Scene scene = scope.TestFiber.Root;

            Unit unit = TestHelper.CreateServerUnit(scene, UnitType.Player, campId: 1);
            if (unit == null)
            {
                Log.Console("server unit is null");
                return 1;
            }

            if (unit.GetComponent<TargetSelectorComponent>() != null)
            {
                Log.Console("target selector should not exist initially");
                return 2;
            }

            unit.AddComponent<TargetSelectorComponent>();
            TargetSelectorComponent selector = unit.GetComponent<TargetSelectorComponent>();
            if (selector == null)
            {
                Log.Console("target selector add failed");
                return 3;
            }

            if (selector.CurrentTargetId != 0)
            {
                Log.Console($"current target should be 0, actual={selector.CurrentTargetId}");
                return 4;
            }

            if (selector.LastSelectTime != 0)
            {
                Log.Console($"last select time should be 0, actual={selector.LastSelectTime}");
                return 5;
            }

            if (selector.SelectIntervalMs != 1000)
            {
                Log.Console($"select interval should be 1000, actual={selector.SelectIntervalMs}");
                return 6;
            }

            if (selector.MaxRange != 10f)
            {
                Log.Console($"max range should be 10, actual={selector.MaxRange}");
                return 7;
            }

            if (selector.LastLineOfSightCheckTime != 0)
            {
                Log.Console($"last los check time should be 0, actual={selector.LastLineOfSightCheckTime}");
                return 8;
            }

            if (selector.LastLineOfSightTargetId != 0)
            {
                Log.Console($"last los target id should be 0, actual={selector.LastLineOfSightTargetId}");
                return 9;
            }

            if (selector.LastLineOfSightPassed)
            {
                Log.Console("last los passed should be false initially");
                return 10;
            }

            if (selector.ConsecutiveLineOfSightBlockedCount != 0)
            {
                Log.Console($"los blocked count should be 0, actual={selector.ConsecutiveLineOfSightBlockedCount}");
                return 11;
            }

            if (selector.GetCurrentTarget() != null)
            {
                Log.Console("current target should be null when current target id is 0");
                return 12;
            }

            FieldInfo manualTargetField = typeof(TargetSelectorComponent).GetField("ManualTargetId",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (manualTargetField != null)
            {
                Log.Console("manual target field should be removed");
                return 13;
            }

            MethodInfo setManualTargetMethod = typeof(TargetSelectorComponentSystem).GetMethod("SetManualTarget",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (setManualTargetMethod != null)
            {
                Log.Console("SetManualTarget method should be removed");
                return 14;
            }

            Log.Console("Test_TargetSelector_Basic_Test PASSED");
            return ErrorCode.ERR_Success;
        }
    }
}
