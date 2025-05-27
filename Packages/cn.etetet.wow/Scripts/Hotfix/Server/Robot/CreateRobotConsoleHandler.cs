using System;
using System.Collections.Generic;
using CommandLine;
using MongoDB.Bson;
using Unity.Mathematics;

namespace ET.Server
{
    [ConsoleHandler(ConsoleMode.CreateRobot)]
    public class CreateRobotConsoleHandler: IConsoleHandler
    {
        public async ETTask Run(Fiber fiber, ModeContex contex, string content)
        {
            try
            {
                switch (content)
                {
                    case ConsoleMode.CreateRobot:
                    {
                        Log.Console("CreateRobot args error!");
                        break;
                    }
                    default:
                    {
                        CreateRobotArgs options = null;
                        Parser.Default.ParseArguments<CreateRobotArgs>(content.Split(' '))
                                .WithNotParsed(error => throw new Exception($"CreateRobotArgs error!"))
                                .WithParsed(o => { options = o; });

                        RobotManagerComponent robotManagerComponent =
                                fiber.Root.GetComponent<RobotManagerComponent>() ?? fiber.Root.AddComponent<RobotManagerComponent>();

                        EntityRef<RobotManagerComponent> robotManagerComponentRef = robotManagerComponent;
                    
                        // 创建机器人
                        TimerComponent timerComponent = fiber.Root.GetComponent<TimerComponent>();
                        for (int i = 0; i < options.Num; ++i)
                        {
                            robotManagerComponent = robotManagerComponentRef;
                            await robotManagerComponent.NewRobot($"Robot_{i}");
                            Log.Console($"Create Robot_{i}");
                        }
                        break;
                    }
                }
            }
            finally
            {
                contex.Parent.RemoveComponent<ModeContex>();
            }
        }
    }
}