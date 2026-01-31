using System;
using System.Collections.Generic;
using System.Diagnostics;
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

                if (testHandlers.Count == 0)
                {
                    Log.Console("not found test!");
                    Environment.Exit(1);
                }
                
                Stopwatch stopwatch = Stopwatch.StartNew();
                List<string> failedTests = new();
                List<string> passedTests = new();
                int exitCode = 0;
                foreach (ITestHandler testHandler in testHandlers)
                {
                    Type testType = testHandler.GetType();
                    string testName = testType.Name;
                    Log.Console($"--------------------------------------------------------------------");
                    Log.Console($"\u001b[34m{testName} start\u001b[0m");
                    try
                    {
                        int ret = await testHandler.Handle(new TestContext() { Fiber = fiber, Args = options });
                        if (ret == 0)
                        {
                            passedTests.Add(testName);
                            Log.Console($"\u001b[32m{testName} success\u001b[0m");
                        }
                        else
                        {
                            exitCode = 1;
                            failedTests.Add(testName);
                            Log.Console($"\u001b[31m{testName} fail! ret: {ret}\u001b[0m");
                        }
                    }
                    catch (Exception e)
                    {
                        exitCode = 1;
                        failedTests.Add(testName);
                        Log.Console($"\u001b[31m{testName} fail!\n{e}\u001b[0m");
                    }
                }
                stopwatch.Stop();
                
                Log.Console($"--------------------------------------------------------------------");
                Log.Console("Test Summary:");
                Log.Console($"Total: {testHandlers.Count}, Passed: {passedTests.Count}, Failed: {failedTests.Count}, Time: {stopwatch.ElapsedMilliseconds}ms");
                if (failedTests.Count > 0)
                {
                    Log.Console("Failed Tests:");
                    foreach (string failedTest in failedTests)
                    {
                        Log.Console($"- {failedTest}");
                    }
                }
                Environment.Exit(exitCode);
            }
            catch (Exception e)
            {
                Log.Console(e.ToString());
                Environment.Exit(1);
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
