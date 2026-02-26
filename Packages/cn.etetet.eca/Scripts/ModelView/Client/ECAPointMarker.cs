using Sirenix.OdinInspector;
using UnityEngine;

namespace ET.Client
{
    [EnableClass]
    public class ECAPointMarker : SerializedMonoBehaviour
    {
        [Header("基础配置")]
        public ECAConfigAsset Config;
        
        [Range(1f, 20f)]
        public float InteractRange = 5f;
        
        [Header("可视化")]
        public bool ShowRange = true;
        public Color RangeColor = Color.green;
        
        private void OnValidate()
        {
            if (Config != null)
            {
                Config.InteractRange = InteractRange;
            }
        }
        
        private void OnDrawGizmos()
        {
            if (ShowRange)
            {
                Gizmos.color = RangeColor;
                Gizmos.DrawWireSphere(transform.position, InteractRange);
            }
        }
        
        public ECAConfig GetConfig()
        {
            if (Config == null)
            {
                Debug.LogError($"ECAPointMarker at {transform.position} has no config!");
                return null;
            }
            
            return Config.ToRuntimeConfig(transform.position);
        }
    }
}
