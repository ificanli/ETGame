using UnityEngine.Serialization;

namespace ET
{
    [System.Serializable]
    [EnableClass]
    public class EffectConfig
    {
#if UNITY
        [UnityEngine.SerializeReference]
        [Sirenix.OdinInspector.HideLabel]
#endif
        public BTNode Node;
    }
}