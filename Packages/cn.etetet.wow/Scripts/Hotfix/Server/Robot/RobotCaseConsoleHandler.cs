using System;
using CommandLine;

namespace ET.Server
{
    /// <summary>
    /// 机器人测试用例控制台处理器
    /// 负责解析控制台命令并分发给对应的RobotCase Handler执行
    /// </summary>
    [ConsoleHandler(ConsoleMode.Case)]
    public class RobotCaseConsoleHandler : IConsoleHandler
    {
        public async ETTask Run(Fiber fiber, ModeContex contex, string content)
        {
            EntityRef<ModeContex> modeContexRef = contex;
            try
            {
                if (content == ConsoleMode.Case)
                {
                    Log.Console("RobotCase args error!");
                    return;
                }
                
                // 解析命令行参数
                RobotCaseArgs options = null;
                Parser.Default.ParseArguments<RobotCaseArgs>(content.Split(' '))
                        .WithNotParsed(error => throw new Exception($"RobotCaseArgs解析错误!"))
                        .WithParsed(o => { options = o; });
                
                if (options.Id == 0)
                {
                    // 执行所有注册的测试用例
                    var list = EventSystem.Instance.GetAllInvokerTypes<RobotCaseContext>();
                    foreach (long type in list)
                    {
                        try
                        {
                            int ret = await EventSystem.Instance.Invoke<RobotCaseContext, ETTask<int>>(type, new RobotCaseContext() { Args = options });
                            if (ret != ErrorCode.ERR_Success)
                            {
                                Log.Console($"case run failed: {type}");
                                return;
                            }
                        }
                        catch (Exception e)
                        {
                            throw new Exception($"case run failed: {type}", e);
                        }
                    }

                    Log.Console($"case run success: 0");
                }
                else
                {
                    int type = options.Id;
                    try
                    {
                        // 执行options.Case这个测试用例
                        int ret = await EventSystem.Instance.Invoke<RobotCaseContext, ETTask<int>>(type, new RobotCaseContext() { Args = options });
                        if (ret != ErrorCode.ERR_Success)
                        {
                            Log.Console($"case run failed: {type}");
                        }
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"case run failed: {type}", e);
                    }

                    Log.Console($"case run success: {type}");
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