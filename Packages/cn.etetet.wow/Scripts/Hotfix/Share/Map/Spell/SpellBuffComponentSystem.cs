using ET.Server;

namespace ET
{
    [EntitySystemOf(typeof(SpellBuffComponent))]
    public static partial class SpellBuffComponentSystem
    {
        [EntitySystem]
        private static void Awake(this SpellBuffComponent self)
        {

        }
        
        [EntitySystem]
        private static void Destroy(this SpellBuffComponent self)
        {
            Unit unit = self.Parent.Parent.GetParent<Unit>();
            foreach (long buff in self.Buffs)
            {
                BuffHelper.RemoveBuff(unit, buff, BuffFlags.CurrentSpellRemoveRemove);
            }
        }

    }
}