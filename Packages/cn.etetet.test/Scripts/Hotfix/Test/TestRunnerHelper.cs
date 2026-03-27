using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace ET.Test
{
    public static class TestRunnerHelper
    {
        public static async ETTask<int> Run(Fiber fiber, TestArgs options)
        {
            options ??= new TestArgs();
            options.Name = string.IsNullOrWhiteSpace(options.Name) ? ".*" : options.Name;

            List<ITestHandler> testHandlers = TestDispatcher.Instance.Get(options.Name);
            if (testHandlers.Count == 0)
            {
                Log.Console("not found test!");
                return 1;
            }

            Stopwatch stopwatch = Stopwatch.StartNew();
            List<string> failedTests = new();
            List<string> passedTests = new();
            int exitCode = 0;

            foreach (ITestHandler testHandler in testHandlers)
            {
                Type testType = testHandler.GetType();
                string testName = testType.Name;
                Log.Console("--------------------------------------------------------------------");
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
            Log.Console("--------------------------------------------------------------------");
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

            return exitCode;
        }

        public static async ETTask RunAndExit(Fiber fiber, TestArgs options)
        {
            try
            {
                int exitCode = await Run(fiber, options);
                Environment.Exit(exitCode);
            }
            catch (Exception e)
            {
                Log.Console(e.ToString());
                Environment.Exit(1);
            }
        }
    }
}
