namespace ET.Client
{
    public static class UnitClickHelper
    {
        /// <summary>
        /// click-unit 旧链路兼容壳。
        /// 局内交互统一改为按钮触发，不再通过直点场景单位发送请求。
        /// </summary>
        public static async ETTask Click(Scene root, long unitId)
        {
            await ETTask.CompletedTask;
        }
    }
}
