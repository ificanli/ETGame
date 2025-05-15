using System;
using UnityEngine;
using YIUIFramework;

namespace ET.Client
{
    public static partial class YIUIPanelComponentSystem
    {
        public static async ETTask<T> OpenViewAsync<T>(this YIUIPanelComponent self)
                where T : Entity
        {
            EntityRef<YIUIPanelComponent> selfRef = self;
            Entity view = await self.GetView<T>();
            if (view == null) return default;
            EntityRef<Entity> viewRef = view;
            var success       = false;
            var component     = (T)view;
            var viewComponent = component.GetParent<YIUIChild>().GetComponent<YIUIViewComponent>();
            EntityRef<YIUIViewComponent> viewComponentRef = viewComponent;
            self = selfRef;
            await self.OpenViewBefore(view);

            try
            {
                viewComponent = viewComponentRef;
                success = await viewComponent.Open();
            }
            catch (Exception e)
            {
                Debug.LogError($"err={e.Message}{e.StackTrace}");
            }

            self = selfRef;

            view = viewRef;
            await self.OpenViewAfter(view, success);

            return component;
        }

        public static async ETTask<T> OpenViewParamAsync<T>(this YIUIPanelComponent self, params object[] paramMore)
                where T : Entity, IYIUIOpen<ParamVo>
        {
            EntityRef<YIUIPanelComponent> selfRef = self;
            var view = await self.GetView<T>();
            
            if (view == null) return default;
            EntityRef<Entity> viewRef = view;
            var success       = false;
            var component     = (T)view;
            var viewComponent = component.GetParent<YIUIChild>().GetComponent<YIUIViewComponent>();
            EntityRef<YIUIViewComponent> viewComponentRef = viewComponent;
            self = selfRef;
            await self.OpenViewBefore(view);

            var p = ParamVo.Get(paramMore);

            try
            {
                viewComponent = viewComponentRef;
                success = await viewComponent.Open(p);
            }
            catch (Exception e)
            {
                Debug.LogError($"err={e.Message}{e.StackTrace}");
            }

            self = selfRef;
            view = viewRef;
            await self.OpenViewAfter(view, success);

            ParamVo.Put(p);

            return component;
        }

        public static async ETTask<T> OpenViewAsync<T, P1>(this YIUIPanelComponent self, P1 p1)
                where T : Entity, IYIUIOpen<P1>
        {
            EntityRef<YIUIPanelComponent> selfRef = self;
            var view = await self.GetView<T>();
            if (view == null) return default;
            EntityRef<Entity> viewRef = view;

            var success       = false;
            var component     = (T)view;
            var viewComponent = component.GetParent<YIUIChild>().GetComponent<YIUIViewComponent>();
            EntityRef<YIUIViewComponent> viewComponentRef = viewComponent;
            self = selfRef;
            await self.OpenViewBefore(view);

            try
            {
                viewComponent = viewComponentRef;
                success = await viewComponent.Open(p1);
            }
            catch (Exception e)
            {
                Debug.LogError($"err={e.Message}{e.StackTrace}");
            }

            self = selfRef;
            view = viewRef;
            await self.OpenViewAfter(view, success);

            return component;
        }

        public static async ETTask<T> OpenViewAsync<T, P1, P2>(this YIUIPanelComponent self, P1 p1, P2 p2)
                where T : Entity, IYIUIOpen<P1, P2>
        {
            EntityRef<YIUIPanelComponent> selfRef = self;
            var view = await self.GetView<T>();
            if (view == null) return default;

            EntityRef<Entity> viewRef = view;
            var success       = false;
            var component     = (T)view;
            var viewComponent = component.GetParent<YIUIChild>().GetComponent<YIUIViewComponent>();
            EntityRef<YIUIViewComponent> viewComponentRef = viewComponent;
            self = selfRef;
            await self.OpenViewBefore(view);

            try
            {
                viewComponent = viewComponentRef;
                success = await viewComponent.Open(p1, p2);
            }
            catch (Exception e)
            {
                Debug.LogError($"err={e.Message}{e.StackTrace}");
            }

            self = selfRef;
            view = viewRef;
            await self.OpenViewAfter(view, success);

            return component;
        }

        public static async ETTask<T> OpenViewAsync<T, P1, P2, P3>(this YIUIPanelComponent self, P1 p1, P2 p2, P3 p3)
                where T : Entity, IYIUIOpen<P1, P2, P3>
        {
            EntityRef<YIUIPanelComponent> selfRef = self;
            var view = await self.GetView<T>();
            if (view == null) return default;

            EntityRef<Entity> viewRef = view;
            var success       = false;
            var component     = (T)view;
            var viewComponent = component.GetParent<YIUIChild>().GetComponent<YIUIViewComponent>();
            EntityRef<YIUIViewComponent> viewComponentRef = viewComponent;
            self = selfRef;
            await self.OpenViewBefore(view);

            try
            {
                viewComponent = viewComponentRef;
                success = await viewComponent.Open(p1, p2, p3);
            }
            catch (Exception e)
            {
                Debug.LogError($"err={e.Message}{e.StackTrace}");
            }

            self = selfRef;
            view = viewRef;
            await self.OpenViewAfter(view, success);

            return component;
        }

        public static async ETTask<T> OpenViewAsync<T, P1, P2, P3, P4>(this YIUIPanelComponent self, P1 p1, P2 p2, P3 p3, P4 p4)
                where T : Entity, IYIUIOpen<P1, P2, P3, P4>
        {
            EntityRef<YIUIPanelComponent> selfRef = self;
            var view = await self.GetView<T>();
            if (view == null) return default;

            EntityRef<Entity> viewRef = view;
            var success       = false;
            var component     = (T)view;
            var viewComponent = component.GetParent<YIUIChild>().GetComponent<YIUIViewComponent>();
            EntityRef<YIUIViewComponent> viewComponentRef = viewComponent;
            self = selfRef;
            await self.OpenViewBefore(view);

            try
            {
                viewComponent = viewComponentRef;
                success = await viewComponent.Open(p1, p2, p3, p4);
            }
            catch (Exception e)
            {
                Debug.LogError($"err={e.Message}{e.StackTrace}");
            }

            self = selfRef;
            view = viewRef;
            await self.OpenViewAfter(view, success);

            return component;
        }

        public static async ETTask<T> OpenViewAsync<T, P1, P2, P3, P4, P5>(this YIUIPanelComponent self, P1 p1, P2 p2, P3 p3, P4 p4,
                                                                           P5                      p5)
                where T : Entity, IYIUIOpen<P1, P2, P3, P4, P5>
        {
            EntityRef<YIUIPanelComponent> selfRef = self;
            var view = await self.GetView<T>();
            if (view == null) return default;
            EntityRef<Entity> viewRef = view;
            var success       = false;
            var component     = (T)view;
            var viewComponent = component.GetParent<YIUIChild>().GetComponent<YIUIViewComponent>();
            EntityRef<YIUIViewComponent> viewComponentRef = viewComponent;
            self = selfRef;
            await self.OpenViewBefore(view);

            try
            {
                viewComponent = viewComponentRef;
                success = await viewComponent.Open(p1, p2, p3, p4, p5);
            }
            catch (Exception e)
            {
                Debug.LogError($"err={e.Message}{e.StackTrace}");
            }

            self = selfRef;
            view = viewRef;
            await self.OpenViewAfter(view, success);

            return component;
        }
    }
}
