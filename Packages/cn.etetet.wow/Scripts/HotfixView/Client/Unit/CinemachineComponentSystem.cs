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
            GameObject virtualCamera = GameObject.Find("/Global/Virtual Camera");
            CinemachineVirtualCamera cinemachineVirtualCamera = virtualCamera.GetComponent<CinemachineVirtualCamera>();
            Transform transform = gameObjectComponent.GameObject.transform;
            cinemachineVirtualCamera.LookAt = gameObjectComponent.GameObject.Get<GameObject>("Head").transform;
            cinemachineVirtualCamera.Follow = transform;
        }
    }
}