using UnityEngine;

namespace ET.Client
{
    [CreateAssetMenu(fileName = "ECAConfig", menuName = "ET/ECA Config")]
    [EnableClass]
    public class ECAConfigAsset : ScriptableObject
    {
        public string ConfigId;
        public int PointType;
        public float InteractRange = 3f;
        
        public ECAConfig ToRuntimeConfig(Vector3 worldPos)
        {
            return new ECAConfig
            {
                ConfigId = this.ConfigId,
                PointType = this.PointType,
                InteractRange = this.InteractRange,
                PosX = worldPos.x,
                PosY = worldPos.y,
                PosZ = worldPos.z
            };
        }
    }
}
