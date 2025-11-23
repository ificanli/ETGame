namespace ET.Client
{
    [MessageHandler(SceneType.Client)]
    public class Robot_LoginRequestHandler: MessageHandler<Scene, Robot_LoginRequest, Robot_LoginResponse>
    {
        protected override async ETTask Run(Scene root, Robot_LoginRequest request, Robot_LoginResponse response)
        {
            EntityRef<Scene> rootRef = root;
            root = rootRef;
            await LoginHelper.Login(root, "127.0.0.1:10101", request.Account, request.Password);
            root = rootRef;
            await EnterMapHelper.EnterMapAsync(root);
        }
    }
}