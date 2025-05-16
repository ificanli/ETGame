using System;

namespace ET.Client
{
    /// <summary>
    /// 打开动画消息
    /// </summary>
    public static partial class YIUIEventSystem
    {
        public static async ETTask<bool> OpenTween(Entity component)
        {
            if (component == null || component.IsDisposed)
            {
                return true;
            }

            var iYIUIOpenTweenSystems = EntitySystemSingleton.Instance.TypeSystems.GetSystems(component.GetType(), typeof(IYIUIOpenTweenSystem));
            if (iYIUIOpenTweenSystems == null)
            {
                return false; //没有则执行默认
            }

            EntityRef<Entity> componentRef = component;
            foreach (IYIUIOpenTweenSystem aYIUIOpenTweenSystem in iYIUIOpenTweenSystems)
            {
                if (aYIUIOpenTweenSystem == null)
                {
                    continue;
                }

                try
                {
                    component = componentRef;
                    await aYIUIOpenTweenSystem.Run(component);
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }

            return true;
        }
    }
}
