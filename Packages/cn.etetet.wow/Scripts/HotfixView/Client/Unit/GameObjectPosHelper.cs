using UnityEngine;

namespace ET.Client
{
    public static class GameObjectPosHelper
    {
        public static void OnTerrain(Transform transform)
        {
            // 贴地
            Ray ray = new(new Vector3(transform.position.x, transform.position.y + 100, transform.position.z), Vector3.down);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 200, LayerMask.GetMask("Map")))
            {
                transform.position = hit.point;
            }
        }
    }
}