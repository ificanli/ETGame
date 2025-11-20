using System;
using CommandLine;

namespace ET
{
    public class Init
    {
        public void Start()
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    Log.Error(e.ExceptionObject.ToString());
                };

                if (Options.Instance == null)
                {
                    // 命令行参数
                    Parser.Default.ParseArguments<Options>(System.Environment.GetCommandLineArgs())
                            .WithNotParsed(error => throw new Exception($"命令行格式错误! {error}"))
                            .WithParsed((o) => World.Instance.AddSingleton(o));
                }

                // 测试用例使用单线程模式，方便重置测试环境
                if (Options.Instance.SceneName == "Test")
                {
                    Options.Instance.SingleThread = 1;
                }
				
                World.Instance.AddSingleton<Logger>().Log = new NLogger(Options.Instance.SceneName);
				
                ETTask.ExceptionHandler += Log.Error;
                
                World.Instance.AddSingleton<TimeInfo>();
                World.Instance.AddSingleton<FiberManager>();
                World.Instance.AddSingleton<CodeLoader>().Start();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        }

        public void Update()
        {
            FiberManager.Instance.Update();
        }

        public void LateUpdate()
        {
            FiberManager.Instance.LateUpdate();
        }
    }
}