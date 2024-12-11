using ET.Server;

namespace ET.Server
{
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
                BuffHelper.RemoveBuff(buff, BuffFlags.ParentRemoveRemove);
            }
        }

    }
}