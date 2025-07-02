using System;


namespace ET.Client
{
    public static partial class EnterMapHelper
    {
        public static async ETTask EnterMapAsync(Scene root)
        {
            try
            {
                EntityRef<Scene> rootRef = root;
                G2C_EnterMap g2CEnterMap = await root.GetComponent<ClientSenderComponent>().Call(C2G_EnterMap.Create()) as G2C_EnterMap;
                // 等待场景切换完成
                root = rootRef;
                await root.GetComponent<ObjectWait>().Wait<Wait_SceneChangeFinish>();
                root = rootRef;
                EventSystem.Instance.Publish(root, new EnterMapFinish());
            }
            catch (Exception e)
            {
                Log.Error(e);
            }	
        }
    }
}