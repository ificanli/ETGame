using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace ET
{
    [Module(ModuleName.BehaviorTree)]
    public abstract class BTRoot: BTNode
    {
#if UNITY_EDITOR
        [StaticField]
        public static BTRoot OpenNode;

        [StaticField]
        public static UnityEngine.Object ScriptableObject;
#endif
    }
}