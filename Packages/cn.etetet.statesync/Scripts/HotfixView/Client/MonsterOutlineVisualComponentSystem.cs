using UnityEngine;

namespace ET.Client
{
    [Event(SceneType.Current)]
    public class AfterUnitCreate_ApplyMonsterOutline : AEvent<Scene, AfterUnitCreate>
    {
        protected override async ETTask Run(Scene scene, AfterUnitCreate args)
        {
            Unit unit = args.Unit;
            if (!MonsterOutlineVisualHelper.TryResolveStyle(unit, out MonsterOutlineTier tier, out Color color, out float width))
            {
                await ETTask.CompletedTask;
                return;
            }

            MonsterOutlineVisualComponent outlineComponent = unit.GetComponent<MonsterOutlineVisualComponent>();
            if (outlineComponent == null)
            {
                outlineComponent = unit.AddComponent<MonsterOutlineVisualComponent>();
            }

            outlineComponent.Tier = tier;
            outlineComponent.OutlineColor = color;
            outlineComponent.OutlineWidth = width;
            outlineComponent.Applied = false;
            await ETTask.CompletedTask;
        }
    }

    [EntitySystemOf(typeof(MonsterOutlineVisualComponent))]
    public static partial class MonsterOutlineVisualComponentSystem
    {
        [EntitySystem]
        private static void Awake(this MonsterOutlineVisualComponent self)
        {
            self.Tier = MonsterOutlineTier.None;
            self.OutlineColor = Color.clear;
            self.OutlineWidth = 0f;
            self.Applied = false;
        }

        [EntitySystem]
        private static void Destroy(this MonsterOutlineVisualComponent self)
        {
            Unit unit = self.GetParent<Unit>();
            unit?.GetComponent<GameObjectComponent>()?.ClearOutlineVisual();
        }

        [EntitySystem]
        private static void Update(this MonsterOutlineVisualComponent self)
        {
            if (self.Applied)
            {
                return;
            }

            Unit unit = self.GetParent<Unit>();
            GameObjectComponent gameObjectComponent = unit?.GetComponent<GameObjectComponent>();
            if (unit == null || unit.IsDisposed || gameObjectComponent?.GameObject == null)
            {
                return;
            }

            gameObjectComponent.ApplyOutlineVisual(self.OutlineColor, self.OutlineWidth);
            self.Applied = true;
        }
    }

    internal static class MonsterOutlineVisualHelper
    {
        private const int HeavyGatorUnitConfigId = 1015;
        private const int BalooUnitConfigId = 1016;
        private const float NormalOutlineWidth = 0.028f;
        private const float EliteBossOutlineWidth = 0.038f;

        public static bool TryResolveStyle(Unit unit, out MonsterOutlineTier tier, out Color color, out float width)
        {
            tier = MonsterOutlineTier.None;
            color = Color.clear;
            width = 0f;

            if (unit == null || unit.IsDisposed || unit.UnitType != UnitType.Monster)
            {
                return false;
            }

            tier = IsEliteOrBoss(unit) ? MonsterOutlineTier.EliteOrBoss : MonsterOutlineTier.Normal;
            switch (tier)
            {
                case MonsterOutlineTier.EliteOrBoss:
                    color = new Color(1f, 0.55f, 0.12f, 1f);
                    width = EliteBossOutlineWidth;
                    return true;
                case MonsterOutlineTier.Normal:
                    color = new Color(0.92f, 0.15f, 0.15f, 1f);
                    width = NormalOutlineWidth;
                    return true;
                default:
                    return false;
            }
        }

        private static bool IsEliteOrBoss(Unit unit)
        {
            if (unit.ConfigId == HeavyGatorUnitConfigId || unit.ConfigId == BalooUnitConfigId)
            {
                return true;
            }

            UnitConfig config = UnitConfigCategory.Instance.GetOrDefault(unit.ConfigId);
            if (config == null)
            {
                return false;
            }

            return string.Equals(config.Name, "XiaoE", System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(config.Name, "BearBoss", System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(config.Name, "heavy_gator", System.StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(config.Name, "baloo", System.StringComparison.OrdinalIgnoreCase);
        }
    }
}
