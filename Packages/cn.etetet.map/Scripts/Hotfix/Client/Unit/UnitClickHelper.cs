namespace ET.Client
{
    public static class UnitClickHelper
    {
        /// <summary>
        /// 点击NPC，请求服务端，服务端会发送要显示的内容给客户端
        /// </summary>
        public static async ETTask Click(Scene root, long unitId)
        {
            // 点击Unit
            C2M_ClickUnitRequest c2MClickUnitRequest = C2M_ClickUnitRequest.Create();
            c2MClickUnitRequest.UnitId = unitId;
            M2C_ClickUnitResponse response = await root.GetComponent<ClientSenderComponent>().Call(c2MClickUnitRequest) as M2C_ClickUnitResponse;

            // 根据返回的类型做不同的显示
            if (response.questInfo != null && response.questInfo.Count > 0)
            {
                // 显示任务界面
            }
        }
    }
}