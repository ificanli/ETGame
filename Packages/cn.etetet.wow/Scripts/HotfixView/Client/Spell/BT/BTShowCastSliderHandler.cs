using UnityEngine;

namespace ET.Client
{
    public class BTShowCastSliderHandler : ABTHandler<BTShowCastSlider>
    {
        protected override int Run(BTShowCastSlider node, BTEnv env)
        {
            Buff buff   = env.GetEntity<Buff>(node.Buff);
            Unit target = buff.Parent.GetParent<Unit>();

            Log.Info($"读条 {target.Config().Name}, {node.ShowDisplayName}, {buff.GetConfig().Desc}");

            buff.DynamicEvent(new BTEvent_ShowCastSlider
            {
                Unit            = target,
                Buff            = buff,
                ShowDisplayName = node.ShowDisplayName,
                IconName        = node.IconName,
                IsIncrease      = node.IsIncrease
            }).NoContext();

            return 0;
        }
    }
}