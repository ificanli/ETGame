using System;
using System.Reflection;
using CommandLine;
using ET.Server;

namespace ET.Test
{
    /// <summary>
    /// 机器人测试用例控制台处理器
    /// 负责解析控制台命令并分发给对应的Test Handler执行
    /// </summary>
    [ConsoleHandler(ConsoleMode.Test)]
    public class TestConsoleHandler : IConsoleHandler
    {
        public async ETTask Run(Fiber fiber, ModeContex contex, string content)
        {
            EntityRef<ModeContex> modeContexRef = contex;
            try
            {
                TestArgs options = null;
                Parser.Default.ParseArguments<TestArgs>(content.Split(' '))
                        .WithNotParsed(error => throw new Exception($"TestArgs解析错误!"))
                        .WithParsed(o => { options = o; });
                
                var testHandlers = TestDispatcher.Instance.Get(options.Name);

                foreach (ITestHandler testHandler in testHandlers)
                {
                    Type testType = testHandler.GetType();
                    string testName = testType.Name;
                    Log.Console($"{testName} start");
                    try
                    {
                        int ret = await testHandler.Handle(new TestContext() { Fiber = fiber, Args = options });
                        if (ret == 0)
                        {
                            Log.Console($"{testName} success");
                        }
                        else
                        {
                            Log.Console($"{testName} fail! ret: {ret}");
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Console($"{testName} fail!\n{e}");
                    }
                }
            }
            catch (Exception e)
            {
                Log.Console(e.ToString());
            }
            finally
            {
                contex = modeContexRef;
                if (contex != null)
                {
                    contex.Parent.RemoveComponent<ModeContex>();    
                }
            }
        }
    }
}