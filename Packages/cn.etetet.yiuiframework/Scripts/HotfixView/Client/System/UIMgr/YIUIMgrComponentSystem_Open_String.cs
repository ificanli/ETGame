using System;
using UnityEngine;
using YIUIFramework;

namespace ET.Client
{
    public static partial class YIUIMgrComponentSystem
    {
        internal static async ETTask<Entity> OpenPanelAsync(this YIUIMgrComponent self, string componentName, Entity root)
        {
            EntityRef<YIUIMgrComponent> selfRef = self;
            var info = self.GetPanelInfo(componentName);
            if (info == null) return default;

            var panel = await self.OpenPanelStartAsync(componentName, root ?? self);
            if (panel == null) return default;

            var success   = false;
            var component = info.OwnerUIEntity;
            EntityRef<Entity> componentRef = component;
            self = selfRef;
            await self.OpenPanelBefore(info);

            try
            {
                success = await info.UIPanel.Open();
            }
            catch (Exception e)
            {
                Debug.LogError($"panel={info.ResName}, err={e.Message}{e.StackTrace}");
            }

            self = selfRef;
            await self.OpenPanelAfter(info, success);

            component = componentRef;
            return success ? component : null;
        }

        internal static async ETTask<Entity> OpenPanelParamAsync(this YIUIMgrComponent self, string componentName, Entity root, params object[] paramMore)
        {
            EntityRef<YIUIMgrComponent> selfRef = self;
            var info = self.GetPanelInfo(componentName);
            if (info == null) return default;

            var panel = await self.OpenPanelStartAsync(componentName, root ?? self);
            if (panel == null) return default;

            var success   = false;
            var component = info.OwnerUIEntity;
            EntityRef<Entity> componentRef = component;
            self = selfRef;
            await self.OpenPanelBefore(info);
            var p = ParamVo.Get(paramMore);

            try
            {
                success = await info.UIPanel.Open(p);
            }
            catch (Exception e)
            {
                Debug.LogError($"panel={info.ResName}, err={e.Message}{e.StackTrace}");
            }

            self = selfRef;
            await self.OpenPanelAfter(info, success);
            ParamVo.Put(p);

            component = componentRef;
            return success ? component : null;
        }

        internal static async ETTask<Entity> OpenPanelAsync<P1>(this YIUIMgrComponent self, string componentName, Entity root, P1 p1)
        {
            EntityRef<YIUIMgrComponent> selfRef = self;
            var info = self.GetPanelInfo(componentName);
            if (info == null) return default;

            var panel = await self.OpenPanelStartAsync(componentName, root ?? self);
            if (panel == null) return default;

            var success   = false;
            var component = info.OwnerUIEntity;
            EntityRef<Entity> componentRef = component;
            self = selfRef;
            await self.OpenPanelBefore(info);

            try
            {
                success = await info.UIPanel.Open(p1);
            }
            catch (Exception e)
            {
                Debug.LogError($"panel={info.ResName}, err={e.Message}{e.StackTrace}");
            }

            self = selfRef;
            await self.OpenPanelAfter(info, success);

            component = componentRef;
            return success ? component : null;
        }

        internal static async ETTask<Entity> OpenPanelAsync<P1, P2>(this YIUIMgrComponent self, string componentName, Entity root, P1 p1, P2 p2)
        {
            EntityRef<YIUIMgrComponent> selfRef = self;
            var info = self.GetPanelInfo(componentName);
            if (info == null) return default;

            var panel = await self.OpenPanelStartAsync(componentName, root ?? self);
            if (panel == null) return default;

            var success   = false;
            var component = info.OwnerUIEntity;
            EntityRef<Entity> componentRef = component;
            self = selfRef;
            await self.OpenPanelBefore(info);

            try
            {
                success = await info.UIPanel.Open(p1, p2);
            }
            catch (Exception e)
            {
                Debug.LogError($"panel={info.ResName}, err={e.Message}{e.StackTrace}");
            }

            self = selfRef;
            await self.OpenPanelAfter(info, success);

            component = componentRef;
            return success ? component : null;
        }

        internal static async ETTask<Entity> OpenPanelAsync<P1, P2, P3>(this YIUIMgrComponent self, string componentName, Entity root, P1 p1, P2 p2, P3 p3)
        {
            EntityRef<YIUIMgrComponent> selfRef = self;
            var info = self.GetPanelInfo(componentName);
            if (info == null) return default;

            var panel = await self.OpenPanelStartAsync(componentName, root ?? self);
            if (panel == null) return default;

            var success   = false;
            var component = info.OwnerUIEntity;
            EntityRef<Entity> componentRef = component;
            self = selfRef;
            await self.OpenPanelBefore(info);

            try
            {
                success = await info.UIPanel.Open(p1, p2, p3);
            }
            catch (Exception e)
            {
                Debug.LogError($"panel={info.ResName}, err={e.Message}{e.StackTrace}");
            }

            self = selfRef;
            await self.OpenPanelAfter(info, success);

            component = componentRef;
            return success ? component : null;
        }

        internal static async ETTask<Entity> OpenPanelAsync<P1, P2, P3, P4>(this YIUIMgrComponent self, string componentName, Entity root, P1 p1, P2 p2, P3 p3, P4 p4)
        {
            EntityRef<YIUIMgrComponent> selfRef = self;
            var info = self.GetPanelInfo(componentName);
            if (info == null) return default;

            var panel = await self.OpenPanelStartAsync(componentName, root ?? self);
            if (panel == null) return default;

            var success   = false;
            var component = info.OwnerUIEntity;
            EntityRef<Entity> componentRef = component;
            self = selfRef;
            await self.OpenPanelBefore(info);

            try
            {
                success = await info.UIPanel.Open(p1, p2, p3, p4);
            }
            catch (Exception e)
            {
                Debug.LogError($"panel={info.ResName}, err={e.Message}{e.StackTrace}");
            }

            self = selfRef;
            await self.OpenPanelAfter(info, success);

            component = componentRef;
            return success ? component : null;
        }

        internal static async ETTask<Entity> OpenPanelAsync<P1, P2, P3, P4, P5>(this YIUIMgrComponent self, string componentName, Entity root, P1 p1, P2 p2, P3 p3, P4 p4, P5 p5)
        {
            EntityRef<YIUIMgrComponent> selfRef = self;
            var info = self.GetPanelInfo(componentName);
            if (info == null) return default;

            var panel = await self.OpenPanelStartAsync(componentName, root ?? self);
            if (panel == null) return default;

            var success   = false;
            var component = info.OwnerUIEntity;
            EntityRef<Entity> componentRef = component;
            self = selfRef;
            await self.OpenPanelBefore(info);

            try
            {
                success = await info.UIPanel.Open(p1, p2, p3, p4, p5);
            }
            catch (Exception e)
            {
                Debug.LogError($"panel={info.ResName}, err={e.Message}{e.StackTrace}");
            }

            self = selfRef;
            await self.OpenPanelAfter(info, success);

            component = componentRef;
            return success ? component : null;
        }
    }
}
