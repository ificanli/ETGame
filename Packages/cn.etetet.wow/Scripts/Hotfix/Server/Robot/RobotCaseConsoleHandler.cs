using System;
using CommandLine;

namespace ET.Server
{
    /// <summary>
    /// 机器人测试用例控制台处理器
    /// 负责解析控制台命令并分发给对应的RobotCase Handler执行
    /// </summary>
    [ConsoleHandler(ConsoleMode.RobotCase)]
    public class RobotCaseConsoleHandler : IConsoleHandler
    {
        public async ETTask Run(Fiber fiber, ModeContex contex, string content)
        {
            try
            {
                if (content == ConsoleMode.RobotCase)
                {
                    Log.Console("RobotCase args error!");
                    return;
                }

                // 解析命令行参数
                RobotCaseArgs options = null;
                Parser.Default.ParseArguments<RobotCaseArgs>(content.Split(' '))
                        .WithNotParsed(error => throw new Exception($"RobotCaseArgs解析错误!"))
                        .WithParsed(o => { options = o; });
                
                _ = fiber.Root.GetComponent<RobotManagerComponent>() ?? fiber.Root.AddComponent<RobotManagerComponent>();

                if (options.All)
                {
                    // 执行所有注册的测试用例
                    var list = EventSystem.Instance.GetAllInvokerTypes<RobotCaseContext>();
                    foreach (long type in list)
                    {
                        try
                        {
                            int ret = await EventSystem.Instance.Invoke<RobotCaseContext, ETTask<int>>(type, new RobotCaseContext() { Fiber = fiber, Args = options });
                            if (ret != ErrorCode.ERR_Success)
                            {
                                Log.Console($"RobotCase Run Failed, case: {type}");
                                return;
                            }
                        }
                        catch (Exception e)
                        {
                            throw new Exception($"RobotCase Run Failed, case: {type}", e);
                        }
                    }

                    Log.Console($"RobotCase Run success, case all!");
                }
                else
                {
                    int type = options.Case;
                    try
                    {
                        // 执行options.Case这个测试用例
                        int ret = await EventSystem.Instance.Invoke<RobotCaseContext, ETTask<int>>(type, new RobotCaseContext() { Fiber = fiber, Args = options });
                        if (ret != ErrorCode.ERR_Success)
                        {
                            Log.Console($"RobotCase Run Failed, case: {type}");
                        }
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"RobotCase Run Failed, case: {type}", e);
                    }

                    Log.Console($"RobotCase Run success, case {type}");
                }
            }
            catch (Exception e)
            {
                Log.Console(e.ToString());
            }
            finally
            {
                contex.Parent.RemoveComponent<ModeContex>();
            }
        }
    }
}