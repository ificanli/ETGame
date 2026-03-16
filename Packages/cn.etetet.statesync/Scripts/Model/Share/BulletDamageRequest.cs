namespace ET
{
    /// <summary>
    /// 子弹伤害请求事件。由 Share 层子弹逻辑发布，服务端订阅后走 DamageContextHelper 统一管线。
    /// </summary>
    public struct BulletDamageRequest
    {
        public long CasterUnitId;
        public long TargetUnitId;
        public int TargetUnitType;
        public int WeaponId;
        public long BaseDamage;
    }
}
