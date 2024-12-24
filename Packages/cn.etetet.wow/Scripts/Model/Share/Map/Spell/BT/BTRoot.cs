using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace ET
{
    public class BTRoot: BTNodeHasChildren
    {
#if UNITY_EDITOR
        [StaticField]
        public static BTRoot OpenNode;

        [StaticField]
        public static UnityEngine.Object So;
#endif
    }
}