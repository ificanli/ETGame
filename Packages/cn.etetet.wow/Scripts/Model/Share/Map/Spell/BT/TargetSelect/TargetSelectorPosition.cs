using MongoDB.Bson.Serialization.Attributes;
using Sirenix.OdinInspector;

namespace ET
{
    [System.Serializable]
    public class TargetSelectorPosition : TargetSelector
    {
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Pos = BTEvnKey.Pos;

        public int Radius;
        
#if UNITY
        [BsonIgnore]
        [OnValueChanged("OnSpellIndicatorValueChanged")]
        public UnityEngine.GameObject SpellIndicatorGO;
        
        [UnityEngine.HideInInspector]
        public string SpellIndicator;

        private void OnSpellIndicatorValueChanged()
        {
            if (this.SpellIndicatorGO == null)
            {
                this.SpellIndicator = "";
                return;
            }

            this.SpellIndicator = this.SpellIndicatorGO.name;
        }
#endif
    }
}