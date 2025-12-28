using System;
using System.Collections.Generic;
using System.Net;

namespace ET.Server
{
    [EntitySystemOf(typeof(HttpComponent))]
    public static partial class HttpComponentSystem
    {
        [EntitySystem]
        private static void Awake(this HttpComponent self, string address)
        {
            try
            {
                self.Listener = new HttpListener();

                foreach (string s in address.Split(';'))
                {
                    if (s.Trim() == "")
                    {
                        continue;
                    }
                    self.Listener.Prefixes.Add(s);
                }

                self.Listener.Start();

                self.Accept().Coroutine();
            }
            catch (HttpListenerException e)
            {
                throw new Exception($"请先在cmd中运行: netsh http add urlacl url=http://*:你的address中的端口/ user=Everyone, address: {address}", e);
            }
        }
        
        [EntitySystem]
        private static void Destroy(this HttpComponent self)
        {
            if (self.Listener == null)
            {
                return;
            }
            try
            {
                if (self.Listener.IsListening)
                {
                    self.Listener.Stop();
                }
            }
            catch (Exception)
            {
                // ignored
            }
            
            try
            {
                self.Listener.Close();
            }
            catch (ObjectDisposedException)
            {
                // HttpListener已经被释放，忽略异常
            }
            finally
            {
                self.Listener = null;
            }
        }

        private static async ETTask Accept(this HttpComponent self)
        {
            EntityRef<HttpComponent> selfRef = self;
            while (true)
            {
                try
                {
                    self = selfRef;
                    if (self == null)
                    {
                        return;
                    }
                    
                    if (self.Listener == null)
                    {
                        return;
                    }

                    if (!self.Listener.IsListening)
                    {
                        return;
                    }

                    HttpListenerContext context = await self.Listener.GetContextAsync();
                    self = selfRef;
                    if (self == null)
                    {
                        return;
                    }
                    self.Handle(context).Coroutine();
                }
                catch (ObjectDisposedException)
                {
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            }
        }

        private static async ETTask Handle(this HttpComponent self, HttpListenerContext context)
        {
            try
            {
                IHttpHandler handler = HttpDispatcher.Instance.Get(self.IScene.SceneType, context.Request.Url.AbsolutePath);
                await handler.Handle(self.Scene(), context);
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
            finally
            {
                context.Response.Close();    
            }
            
        }
    }
}