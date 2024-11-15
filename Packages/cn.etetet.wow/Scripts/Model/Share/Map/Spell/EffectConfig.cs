using UnityEngine.Serialization;

namespace ET
{
    [System.Serializable]
    [EnableClass]
    public class EffectConfig
    {
        
#if UNITY
        [FormerlySerializedAs("EffectTimeType")]
        [Sirenix.OdinInspector.HideLabel]
#endif
        public BTTimeType btTimeType;

#if UNITY
        [UnityEngine.SerializeReference]
        [Sirenix.OdinInspector.HideLabel]
#endif
        public BTNode Node;
    }
}