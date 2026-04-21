using System.Collections.Generic;

namespace ET.Server
{
    [Invoke(TimerInvokeType.RobotAutoLevelTick)]
    public class RobotAutoLevelTimer : ATimer<MatchRobotComponent>
    {
        protected override void Run(MatchRobotComponent self)
        {
            RobotAutoLevelHelper.Tick(self);
        }
    }

    public static class RobotAutoLevelHelper
    {
        public const int AutoLevelElapsedTimeScalePermille = 800;

        public static void StartAutoLevel(Unit unit, MatchRobotComponent matchRobot)
        {
            if (unit == null || unit.IsDisposed || matchRobot == null)
            {
                return;
            }

            RobotAutoLevelConfigCategory category = RobotAutoLevelConfigCategory.Instance;
            if (category == null || category.DataList == null || category.DataList.Count == 0)
            {
                Log.Info($"[RobotAutoLevel] skip: no config, unitId={unit.Id}");
                return;
            }

            matchRobot.MatchStartTime = TimeInfo.Instance.ServerNow();
            matchRobot.AutoLevelTimerId = unit.Root().TimerComponent.NewRepeatedTimer(
                1000,
                TimerInvokeType.RobotAutoLevelTick,
                matchRobot);
            Log.Info($"[RobotAutoLevel] started, unitId={unit.Id}, timerId={matchRobot.AutoLevelTimerId}");
        }

        public static void Tick(MatchRobotComponent matchRobot)
        {
            if (matchRobot == null || matchRobot.IsDisposed)
            {
                return;
            }

            Unit unit = matchRobot.GetParent<Unit>();
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            long elapsedMs = TimeInfo.Instance.ServerNow() - matchRobot.MatchStartTime;
            int elapsedSec = (int)(elapsedMs / 1000);

            int targetLevel = ResolveTargetLevel(elapsedSec);
            if (targetLevel <= 0)
            {
                return;
            }

            RogueProgressComponent progress = unit.GetComponent<RogueProgressComponent>();
            if (progress == null || progress.Level >= targetLevel)
            {
                return;
            }

            EnsureLevelTo(unit, progress, targetLevel);
        }

        public static int ResolveTargetLevel(int elapsedSec)
        {
            RobotAutoLevelConfigCategory category = RobotAutoLevelConfigCategory.Instance;
            if (category == null || category.DataList == null || category.DataList.Count == 0)
            {
                return 0;
            }

            elapsedSec = GetScaledElapsedSec(elapsedSec);
            List<RobotAutoLevelConfig> dataList = category.DataList;

            // 按 TimeSec 升序假设数据已排好序（Id 递增）
            RobotAutoLevelConfig prev = null;
            RobotAutoLevelConfig next = null;

            for (int i = 0; i < dataList.Count; i++)
            {
                RobotAutoLevelConfig entry = dataList[i];
                if (entry.TimeSec <= elapsedSec)
                {
                    prev = entry;
                }
                else
                {
                    next = entry;
                    break;
                }
            }

            if (prev == null)
            {
                return 0;
            }

            // 超过最后一条，返回最大等级
            if (next == null)
            {
                return prev.Level;
            }

            // 线性插值
            int timeDelta = next.TimeSec - prev.TimeSec;
            if (timeDelta <= 0)
            {
                return prev.Level;
            }

            float t = (float)(elapsedSec - prev.TimeSec) / timeDelta;
            int interpolated = prev.Level + (int)((next.Level - prev.Level) * t);
            return interpolated;
        }

        public static int GetScaledElapsedSec(int elapsedSec)
        {
            if (elapsedSec <= 0)
            {
                return 0;
            }

            return elapsedSec * AutoLevelElapsedTimeScalePermille / 1000;
        }

        private static void EnsureLevelTo(Unit unit, RogueProgressComponent progress, int targetLevel)
        {
            int safetyCounter = 0;
            while (progress.Level < targetLevel && safetyCounter < 100)
            {
                safetyCounter++;
                int expNeeded = progress.NeedExp - progress.CurrentExp;
                if (expNeeded <= 0)
                {
                    expNeeded = 1;
                }

                RogueProgressHelper.AddExp(unit, expNeeded);
            }

            if (safetyCounter > 0)
            {
                Log.Info($"[RobotAutoLevel] leveled up, unitId={unit.Id}, level={progress.Level}, target={targetLevel}, iterations={safetyCounter}");
            }
        }
    }
}
