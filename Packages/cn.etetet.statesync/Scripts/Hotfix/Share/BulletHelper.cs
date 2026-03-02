using Unity.Mathematics;

namespace ET
{
    /// <summary>
    /// 子弹工厂辅助类，用于创建子弹实体
    /// </summary>
    public static class BulletHelper
    {
        /// <summary>
        /// 创建子弹
        /// </summary>
        /// <param name="scene">场景</param>
        /// <param name="owner">射击者</param>
        /// <param name="target">目标</param>
        /// <param name="damage">伤害值</param>
        /// <returns>子弹Unit</returns>
        public static Unit CreateBullet(Scene scene, Unit owner, Unit target, float damage)
        {
            UnitComponent unitComponent = scene.GetComponent<UnitComponent>();
            if (unitComponent == null)
            {
                return null;
            }

            // 创建子弹Unit
            Unit bullet = unitComponent.AddChildWithId<Unit, int>(IdGenerater.Instance.GenerateId(), 1);
            bullet.Position = owner.Position;
            bullet.Rotation = quaternion.identity;

            // 添加子弹组件
            bullet.AddComponent<BulletComponent, long, long, float>(owner.Id, target.Id, damage);

            return bullet;
        }
    }
}
