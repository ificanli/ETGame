using UnityEngine;

namespace ET.Client
{
    [ComponentOf(typeof(Unit))]
    public class CinemachineComponent: Entity, IAwake
    {
        public Cinemachine.CinemachineFreeLook FreeLook;
        public Cinemachine.CinemachineVirtualCamera VirtualCamera;

        public Transform Follow { get; set; }

        public Transform Head { get; set; }
    }
}