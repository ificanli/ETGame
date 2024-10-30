using UnityEngine;

namespace ET.Client
{
    [ComponentOf(typeof(Unit))]
    public partial class PlayerControlComponent: Entity, IAwake, IUpdate
    {
        public PlayerControl PlayerControl;
        public long PressTime;

        public EntityRef<CinemachineComponent> CinemachineComponent;
    }
}