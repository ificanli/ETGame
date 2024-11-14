namespace ET
{
    [System.Serializable]
    [EnableClass]
    public class EffectConfig
    {
        
#if UNITY
        [Sirenix.OdinInspector.HideLabel]
#endif
        public EffectTimeType EffectTimeType;

#if UNITY
        [UnityEngine.SerializeReference]
        [Sirenix.OdinInspector.HideLabel]
#endif
        public BTNode Node;
    }
}