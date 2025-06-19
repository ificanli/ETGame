#if ENABLE_VIEW && UNITY_EDITOR
using UnityEngine;

namespace ET
{
    public class ComponentView: MonoBehaviour
    {
        public EntityRef<Entity> Component
        {
            get;
            set;
        }
    }
}
#endif