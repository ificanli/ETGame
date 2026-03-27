using Cinemachine;
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
            GameObject virtualCameraObject = GameObject.Find("/Global/Virtual Camera");
            GameObject followRootObject = GameObject.Find("Global/Unit");
            if (gameObjectComponent?.GameObject == null || virtualCameraObject == null || followRootObject == null)
            {
                Log.Warning("[Camera] missing camera bootstrap objects");
                return;
            }

            CinemachineVirtualCamera cinemachineVirtualCamera = virtualCameraObject.GetComponent<CinemachineVirtualCamera>();
            BindPointComponent bindPointComponent = gameObjectComponent.GameObject.GetComponent<BindPointComponent>();
            if (cinemachineVirtualCamera == null || bindPointComponent == null ||
                !bindPointComponent.GetBindPoints().TryGetValue(BindPoint.Head, out Transform headbone) ||
                headbone == null)
            {
                Log.Warning("[Camera] failed to resolve virtual camera or head bind point");
                return;
            }

            self.VirtualCamera = cinemachineVirtualCamera;
            self.Head = headbone;
            self.BaseRotation = cinemachineVirtualCamera.transform.rotation;
            self.BaseDistance = cinemachineVirtualCamera.GetCinemachineComponent<Cinemachine3rdPersonFollow>()?.CameraDistance ?? 0f;
            self.FollowOffset = Vector3.zero;
            self.Follow = new GameObject("CameraFollow").transform;
            self.Follow.SetParent(followRootObject.transform, true);

            cinemachineVirtualCamera.LookAt = self.Follow;
            cinemachineVirtualCamera.Follow = self.Follow;
            self.ApplyFollowTransform();
        }

        [EntitySystem]
        private static void Destroy(this CinemachineComponent self)
        {
            if (self.VirtualCamera != null)
            {
                if (self.VirtualCamera.LookAt == self.Follow)
                {
                    self.VirtualCamera.LookAt = null;
                }

                if (self.VirtualCamera.Follow == self.Follow)
                {
                    self.VirtualCamera.Follow = null;
                }
            }

            if (self.Follow != null)
            {
                UnityEngine.Object.Destroy(self.Follow.gameObject);
            }

            self.VirtualCamera = null;
            self.Follow = null;
            self.Head = null;
            self.FollowOffset = Vector3.zero;
            self.BaseDistance = 0f;
            self.BaseRotation = Quaternion.identity;
        }

        public static void ResetFollowOffset(this CinemachineComponent self)
        {
            if (self == null)
            {
                return;
            }

            self.FollowOffset = Vector3.zero;
            self.ApplyFollowTransform();
        }

        public static void SetFollowOffset(this CinemachineComponent self, Vector3 worldOffset)
        {
            if (self == null)
            {
                return;
            }

            self.FollowOffset = worldOffset;
            self.ApplyFollowTransform();
        }

        public static void ApplyFollowTransform(this CinemachineComponent self)
        {
            if (self?.Follow == null || self.Head == null)
            {
                return;
            }

            self.Follow.position = self.Head.position + self.FollowOffset;
            self.Follow.rotation = self.BaseRotation;
        }
    }
}
