using System;
using CommandLine;
using UnityEngine;

namespace ET
{
    public class Init: MonoBehaviour
    {
        private void Start()
        {
            this.StartAsync().NoContext();
        }
		
        private async ETTask StartAsync()
        {
            DontDestroyOnLoad(gameObject);
			
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Log.Error(e.ExceptionObject.ToString());
            };

            GlobalConfig globalConfig = Resources.Load<GlobalConfig>("GlobalConfig");
            // 命令行参数
            string[] args = { $"--SceneName={globalConfig.SceneName}" };
            Parser.Default.ParseArguments<Options>(args)
                    .WithNotParsed(error => throw new Exception($"命令行格式错误! {error}"))
                    .WithParsed((o)=>World.Instance.AddSingleton(o));


            
            // 编辑器模式下如果开启了ENABLE_VIEW使用单线程，WEBGL模式也使用单线程
#if (ENABLE_VIEW && UNITY_EDITOR) || UNITY_WEBGL
            Options.Instance.SingleThread = 1;
#endif
            
            World.Instance.AddSingleton<Logger>().Log = new UnityLogger("None");
            ETTask.ExceptionHandler += Log.Error;
			
            World.Instance.AddSingleton<TimeInfo>();
            World.Instance.AddSingleton<FiberManager>();

            await World.Instance.AddSingleton<ResourcesComponent>().CreatePackageAsync("DefaultPackage", true);
            
            World.Instance.AddSingleton<CodeLoader>().Start().NoContext();
        }

        private void Update()
        {
            TimeInfo.Instance.Update();
            FiberManager.Instance.Update();
        }

        private void LateUpdate()
        {
            FiberManager.Instance.LateUpdate();
        }

        private void OnApplicationQuit()
        {
            World.Instance.Dispose();
        }
    }
}