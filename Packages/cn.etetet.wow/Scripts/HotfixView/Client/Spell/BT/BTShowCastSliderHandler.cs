using UnityEngine;

namespace ET.Client
{
    public class BTShowCastSliderHandler: ABTHandler<BTShowCastSlider>
    {
        protected override int Run(BTShowCastSlider node, BTEnv env)
        {
            Buff buff = env.GetEntity<Buff>(node.Buff);
            
            // 这里根据Buff的创建时间，过期时间，当前时间来调整CastSlider
            
            return 0;
        }
    }
}