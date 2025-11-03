namespace ET.Client
{
    /// <summary>
    /// 客户端物品实体System
    /// </summary>
    [EntitySystemOf(typeof(Item))]
    public static partial class ItemSystem
    {
        #region 生命周期方法

        [EntitySystem]
        private static void Awake(this Item self)
        {
        }

        [EntitySystem]
        private static void Destroy(this Item self)
        {
        }

        #endregion
    }
}