using ET.Client;

namespace ET.Server
{
    [MessageHandler(SceneType.Robot)]
    public class Console2Robot_LogoutRequestHandler: MessageHandler<Scene, Console2Robot_LogoutRequest, Console2Robot_LogoutResponse>
    {
        protected override async ETTask Run(Scene root, Console2Robot_LogoutRequest request, Console2Robot_LogoutResponse response)
        {
            C2G_LoginOut c2GLoginOut = C2G_LoginOut.Create();
            G2C_LoginOut g2CLoginOut = await root.GetComponent<ClientSenderComponent>().Call(c2GLoginOut) as G2C_LoginOut;
        }
    }
}