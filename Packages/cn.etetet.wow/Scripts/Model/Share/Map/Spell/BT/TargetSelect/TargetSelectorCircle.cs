using System;
using System.Collections.Generic;
using MongoDB.Bson.Serialization.Attributes;
using Sirenix.OdinInspector;

namespace ET
{
    [System.Serializable]
    public class TargetSelectorCircle : TargetSelector
    {
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(Unit))]
        public string Unit = BTEvnKey.Unit;
        
        
        [Sirenix.OdinInspector.BoxGroup("输出参数")]
        [BTOutput(typeof(List<long>))]
        public string Units = BTEvnKey.Units;
        
        public int Radius;
        
        public UnitType UnitType;
        
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