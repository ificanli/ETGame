using UnityEngine;

namespace ET.Client
{
    /// <summary>
    /// 本地玩家战斗提示组件，负责攻击范围圈和当前攻击目标红圈的运行时状态。
    /// </summary>
    [ComponentOf(typeof(Unit))]
    public class CombatIndicatorComponent : Entity, IAwake, IUpdate, IDestroy
    {
        public GameObject RangeIndicatorObject;
        public Vector3 RangeIndicatorBaseScale;
        public GameObject TargetIndicatorObject;
        public Vector3 TargetIndicatorBaseScale;
        public Transform IndicatorRoot;
        public long CurrentTargetUnitId;
        public int CurrentWeaponId;
        public float CurrentAttackRange;
        public int LoadVersion;
        public bool IsInitialized;
    }

    [ComponentOf(typeof(Unit))]
    public class RogueScaleVisualComponent : Entity, IAwake, IUpdate, IDestroy
    {
        public Vector3 BaseScale;
        public int AppliedScalePermille;
        public bool IsInitialized;
    }
}
