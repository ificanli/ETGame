using System;
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

                await TestRunnerHelper.RunAndExit(fiber, options);
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
