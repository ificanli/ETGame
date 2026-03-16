using UnityEngine;

namespace ET.Client
{
    public static class CombatTextHelper
    {
        private const long MinDisplayValue = 1;
        private const int CriticalDamageThresholdPercent = 15;

        public static void ShowHpChange(Scene scene, Unit unit, long oldHp, long newHp)
        {
            if (scene == null || unit == null || unit.IsDisposed)
            {
                return;
            }

            long delta = newHp - oldHp;
            if (delta == 0)
            {
                return;
            }

            CombatTextComponent combatTextComponent = GetOrCreateCombatTextComponent(scene);
            if (combatTextComponent == null)
            {
                return;
            }

            var unitPos = unit.Position;
            Vector3 worldPos = new Vector3(unitPos.x, unitPos.y + 2.0f, unitPos.z);

            if (delta < 0)
            {
                long damageValue = -delta;
                if (damageValue < MinDisplayValue)
                {
                    return;
                }

                bool isCritical = IsCriticalDamage(unit, damageValue);
                combatTextComponent.Show(worldPos, FormatValue(damageValue),
                    isCritical ? new Color(1.0f, 0.76f, 0.2f, 1.0f) : new Color(1.0f, 0.2f, 0.2f, 1.0f),
                    isCritical ? 52 : 44,
                    isCritical);
                return;
            }

            long healValue = delta;

            combatTextComponent.Show(worldPos, $"+{FormatValue(healValue)}", new Color(0.2f, 1.0f, 0.35f, 1.0f), 42, false);
        }

        private static CombatTextComponent GetOrCreateCombatTextComponent(Scene scene)
        {
            CombatTextComponent component = scene.GetComponent<CombatTextComponent>();
            if (component != null)
            {
                return component;
            }

            return scene.AddComponent<CombatTextComponent>();
        }

        private static string FormatValue(long value)
        {
            if (value >= 1000000)
            {
                return $"{value / 1000000f:0.#}M";
            }

            if (value >= 1000)
            {
                return $"{value / 1000f:0.#}K";
            }

            return value.ToString();
        }

        private static bool IsCriticalDamage(Unit unit, long damageValue)
        {
            if (damageValue <= 0)
            {
                return false;
            }

            NumericComponent numericComponent = unit.NumericComponent;
            if (numericComponent == null)
            {
                return false;
            }

            long maxHp = numericComponent.GetAsLong(NumericType.MaxHP);
            if (maxHp <= 0)
            {
                return false;
            }

            return damageValue * 100 >= maxHp * CriticalDamageThresholdPercent;
        }
    }
}
