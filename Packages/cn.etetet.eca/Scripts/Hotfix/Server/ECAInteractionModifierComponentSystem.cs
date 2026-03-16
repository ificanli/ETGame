namespace ET.Server
{
    [EntitySystemOf(typeof(ECAInteractionModifierComponent))]
    public static partial class ECAInteractionModifierComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ECAInteractionModifierComponent self)
        {
            self.InteractRangeBonus = 0f;
            self.SilentSearchRefCount = 0;
        }

        [EntitySystem]
        private static void Destroy(this ECAInteractionModifierComponent self)
        {
            self.InteractRangeBonus = 0f;
            self.SilentSearchRefCount = 0;
        }
    }

    public static class ECAInteractionModifierHelper
    {
        public static float GetInteractRangeBonus(Unit unit)
        {
            if (unit == null || unit.IsDisposed)
            {
                return 0f;
            }

            ECAInteractionModifierComponent component = unit.GetComponent<ECAInteractionModifierComponent>();
            if (component == null || component.InteractRangeBonus <= 0f)
            {
                return 0f;
            }

            return component.InteractRangeBonus;
        }

        public static void AddInteractRangeBonus(Unit unit, float delta)
        {
            if (unit == null || unit.IsDisposed || delta <= 0f)
            {
                return;
            }

            ECAInteractionModifierComponent component = unit.GetComponent<ECAInteractionModifierComponent>();
            if (component == null)
            {
                component = unit.AddComponent<ECAInteractionModifierComponent>();
            }

            component.InteractRangeBonus += delta;
            if (component.InteractRangeBonus < 0f)
            {
                component.InteractRangeBonus = 0f;
            }
        }

        public static void RemoveInteractRangeBonus(Unit unit, float delta)
        {
            if (unit == null || unit.IsDisposed || delta <= 0f)
            {
                return;
            }

            ECAInteractionModifierComponent component = unit.GetComponent<ECAInteractionModifierComponent>();
            if (component == null)
            {
                return;
            }

            component.InteractRangeBonus -= delta;
            if (component.InteractRangeBonus < 0f)
            {
                component.InteractRangeBonus = 0f;
            }

            TryRemoveIfEmpty(unit, component);
        }

        public static bool HasSilentSearch(Unit unit)
        {
            if (unit == null || unit.IsDisposed)
            {
                return false;
            }

            ECAInteractionModifierComponent component = unit.GetComponent<ECAInteractionModifierComponent>();
            return component != null && component.SilentSearchRefCount > 0;
        }

        public static void AddSilentSearch(Unit unit)
        {
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            ECAInteractionModifierComponent component = unit.GetComponent<ECAInteractionModifierComponent>();
            if (component == null)
            {
                component = unit.AddComponent<ECAInteractionModifierComponent>();
            }

            component.SilentSearchRefCount += 1;
        }

        public static void RemoveSilentSearch(Unit unit)
        {
            if (unit == null || unit.IsDisposed)
            {
                return;
            }

            ECAInteractionModifierComponent component = unit.GetComponent<ECAInteractionModifierComponent>();
            if (component == null)
            {
                return;
            }

            component.SilentSearchRefCount -= 1;
            if (component.SilentSearchRefCount < 0)
            {
                component.SilentSearchRefCount = 0;
            }

            TryRemoveIfEmpty(unit, component);
        }

        private static void TryRemoveIfEmpty(Unit unit, ECAInteractionModifierComponent component)
        {
            if (unit == null || unit.IsDisposed || component == null || component.IsDisposed)
            {
                return;
            }

            if (component.InteractRangeBonus <= 0f && component.SilentSearchRefCount <= 0)
            {
                unit.RemoveComponent<ECAInteractionModifierComponent>();
            }
        }
    }
}
