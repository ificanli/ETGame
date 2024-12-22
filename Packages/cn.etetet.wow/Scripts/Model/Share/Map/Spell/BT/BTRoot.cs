namespace ET
{
    public class BTRoot: BTNode
    {
#if UNITY_EDITOR
        [StaticField]
        public static BTRoot OpenNode;

        [StaticField]
        public static UnityEngine.Object So;
#endif
    }
}