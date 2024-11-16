using Cinemachine;
using Unity.Mathematics;
using UnityEngine;

namespace ET.Client
{
    [EntitySystemOf(typeof(CinemachineComponent))]
    public static partial class CinemachineComponentSystem
    {
        [EntitySystem]
        private static void Awake(this CinemachineComponent self)
        {
            GameObjectComponent gameObjectComponent = self.GetParent<Unit>().GetComponent<GameObjectComponent>();
            
            GameObject virtualCamera = GameObject.Find("/Global/Virtual Camera");
            CinemachineVirtualCamera cinemachineVirtualCamera = virtualCamera.GetComponent<CinemachineVirtualCamera>();
            self.VirtualCamera = cinemachineVirtualCamera;
            
            // 创建跟踪的GameObject
            Transform headbone = gameObjectComponent.GameObject.GetComponent<BindPointComponent>().BindPoints[BindPoint.Head];
            self.Head = headbone;
            self.Follow = new GameObject("CameraFollow").transform;
            Transform followTransform = self.Follow;
            followTransform.SetParent(GameObject.Find("Global/Unit").transform, true);
            followTransform.position = self.Head.position;
            followTransform.rotation = gameObjectComponent.GameObject.transform.rotation;
            
            cinemachineVirtualCamera.LookAt = followTransform;
            cinemachineVirtualCamera.Follow = followTransform;
        }

        public static void RotationFollow(this CinemachineComponent self, Vector2 v)
        {
            Vector3 eulerAngles = self.Follow.transform.eulerAngles;
            v /= 5;
            eulerAngles.x += v.y;
            if (eulerAngles.x >= 360)
            {
                eulerAngles.x -= 360;
            }

            if (eulerAngles.x < 0)
            {
                eulerAngles.x += 360;
            }
            
            eulerAngles.y += v.x;
            if (eulerAngles.y >= 360)
            {
                eulerAngles.y -= 360;
            }

            if (eulerAngles.y < 0)
            {
                eulerAngles.y += 360;
            }

            if (eulerAngles.x < 10)
            {
                eulerAngles.x = 10;
            }

            if (eulerAngles.x > 80)
            {
                eulerAngles.x = 80;
            }
            self.Follow.rotation = Quaternion.Euler(eulerAngles);
        }
    }
}