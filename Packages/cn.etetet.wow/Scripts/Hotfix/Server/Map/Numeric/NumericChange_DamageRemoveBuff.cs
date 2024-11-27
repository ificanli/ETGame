using System.Collections.Generic;

namespace ET.Server
{
    [NumericWatcher(SceneType.Map, (int)NumericType.HP)]
    public class NumericChange_DamageRemoveBuff: INumericWatcher
    {
        public void Run(Unit unit, NumbericChange args)
        {
            if (args.New >= args.Old)
            {
                return;
            }

            BuffComponent buffComponent = unit.GetComponent<BuffComponent>();
            buffComponent.RemoveBuffFlag(BuffFlags.DamageRemove);
        }
    }
}