using ET.Server;

namespace ET.Server
{
    [Module(ModuleName.Spell)]
    [EntitySystemOf(typeof(BuffChildrenComponent))]
    public static partial class BuffChildrenComponentSystem
    {
        [EntitySystem]
        private static void Awake(this BuffChildrenComponent self)
        {

        }
        
        [EntitySystem]
        private static void Destroy(this BuffChildrenComponent self)
        {
            foreach (Buff buff in self.Buffs)
            {
                if (buff == null)
                {
                    continue;
                }
                BuffHelper.RemoveBuff(buff, BuffFlags.ParentRemoveRemove);
            }
        }

    }
}