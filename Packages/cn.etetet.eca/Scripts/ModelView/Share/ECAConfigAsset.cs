using UnityEngine;

namespace ET.Client
{
    [CreateAssetMenu(fileName = "ECAConfig", menuName = "ET/ECA Config")]
    [EnableClass]
    public class ECAConfigAsset : ScriptableObject
    {
        public string ConfigId;
        public int PointType;
        public int TeamId;  // 小队ID（0=不限，1-N=指定小队）
        public float InteractRange = 3f;

        public ECAConfig ToRuntimeConfig(Vector3 worldPos)
        {
            return new ECAConfig
            {
                ConfigId = this.ConfigId,
                PointType = this.PointType,
                TeamId = this.TeamId,
                InteractRange = this.InteractRange,
                PosX = worldPos.x,
                PosY = worldPos.y,
                PosZ = worldPos.z
            };
        }
    }
}
