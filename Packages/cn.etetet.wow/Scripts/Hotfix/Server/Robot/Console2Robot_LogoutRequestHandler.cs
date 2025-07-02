using ET.Client;

namespace ET.Server
{
    [MessageHandler(SceneType.Client)]
    public class Console2Robot_LogoutRequestHandler: MessageHandler<Scene, Console2Robot_LogoutRequest, Console2Robot_LogoutResponse>
    {
        protected override async ETTask Run(Scene root, Console2Robot_LogoutRequest request, Console2Robot_LogoutResponse response)
        {
            C2G_Logout c2GLogout = C2G_Logout.Create();
            G2C_Logout g2CLogout = await root.GetComponent<ClientSenderComponent>().Call(c2GLogout) as G2C_Logout;
        }
    }
}