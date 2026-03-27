using UnityEngine;

namespace ET.Client
{
    [ComponentOf(typeof(Unit))]
    public class CinemachineComponent: Entity, IAwake, IDestroy
    {
        public Cinemachine.CinemachineVirtualCamera VirtualCamera;
        public Quaternion BaseRotation;
        public float BaseDistance;
        public Vector3 FollowOffset;

        public Transform Follow { get; set; }

        public Transform Head { get; set; }
    }
}
