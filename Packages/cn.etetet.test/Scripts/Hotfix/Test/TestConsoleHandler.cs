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
                            Log.Console($"\u001b[32m{testName} success\u001b[0m");
                        }
                        else
                        {
                            Log.Console($"\u001b[31m{testName} fail! ret: {ret}\u001b[0m");
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Console($"\u001b[31m{testName} fail!\n{e}\u001b[0m");
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