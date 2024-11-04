using UnityEngine;

namespace ET.Client
{
    [ComponentOf(typeof(Unit))]
    public partial class InputSystemComponent: Entity, IAwake, IUpdate
    {
        public InputSystem InputSystem;
        public long PressTime;

        public EntityRef<CinemachineComponent> CinemachineComponent;

        public bool IsJumping;
    }
}