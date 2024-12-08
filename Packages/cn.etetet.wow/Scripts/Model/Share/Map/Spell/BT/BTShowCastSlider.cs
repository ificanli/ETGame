using UnityEngine;

namespace ET
{
    public class BTShowCastSlider: BTNode
    {
        [BTInput(typeof(Buff))]
        [Sirenix.OdinInspector.BoxGroup("输入参数")]
        public string Buff;

#if UNITY
        public Sprite Icon;
#endif

        public bool IsIncrease;
    }
}