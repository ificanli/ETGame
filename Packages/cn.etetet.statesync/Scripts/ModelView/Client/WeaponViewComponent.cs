using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 客户端武器表现组件，只负责缓存当前挂载的武器模型和枪口节点。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class WeaponViewComponent : Entity, IAwake, IDestroy
    {
        private GameObject weaponObject;

        public GameObject WeaponObject
        {
            get => this.weaponObject;
            set
            {
                this.weaponObject = value;
                this.MuzzleTransform = null;
            }
        }

        public Transform MuzzleTransform { get; set; }

        public int WeaponId { get; set; }

        public int LoadVersion { get; set; }
    }
}
