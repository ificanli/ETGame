using UnityEngine;

namespace ET.Client
{
    public static class GameObjectPosHelper
    {
        public static Vector3 GetGroundedPosition(Vector3 position)
        {
            Ray ray = new(new Vector3(position.x, position.y + 100f, position.z), Vector3.down);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 200, LayerMask.GetMask("Map")))
            {
                return hit.point;
            }

            return position;
        }

        public static void OnTerrain(Transform transform)
        {
            transform.position = GetGroundedPosition(transform.position);
        }
    }
}
