using System;
using System.Threading;
using System.Threading.Tasks;

namespace ET.Server
{
    [EntitySystemOf(typeof(ConsoleComponent))]
    public static partial class ConsoleComponentSystem
    {
        [EntitySystem]
        private static void Awake(this ConsoleComponent self)
        {
            self.Start().Coroutine();
        }
        
        [EntitySystem]
        private static void Destroy(this ConsoleComponent self)
        {
        }


        private static async ETTask Start(this ConsoleComponent self)
        {
            self.CancellationTokenSource = new CancellationTokenSource();
            EntityRef<ConsoleComponent> selfRef = self;
            while (true)
            {
                try
                {
                    self = selfRef;
                    if (self == null)
                    {
                        return;
                    }
                    ModeContex modeContex = self.GetComponent<ModeContex>();
                    EntityRef<ModeContex> modeContexRef = modeContex;
                    string prefix = $"{modeContex?.Mode ?? ""}> ";
                    string line = await Task.Factory.StartNew(() =>
                    {
                        Console.Write(prefix);
                        string s = Console.In.ReadLine();
                        return s;
                    }, self.CancellationTokenSource.Token);

                    self = selfRef;

                    // 检测到EOF时退出循环
                    if (line == null)
                    {
                        break;
                    }

                    line = line.Trim();

                    
                    switch (line)
                    {
                        case "":
                            break;
                        case "exit":
                            self.RemoveComponent<ModeContex>();
                            break;
                        default:
                            {
                                modeContex = modeContexRef;
                                string[] lines = line.Split(" ");
                                string mode = modeContex == null ? lines[0] : modeContex.Mode;

                                IConsoleHandler iConsoleHandler = ConsoleDispatcher.Instance.Get(mode);
                                if (modeContex == null)
                                {
                                    modeContex = self.AddComponent<ModeContex>();
                                    modeContex.Mode = mode;
                                }

                                await iConsoleHandler.Run(self.Fiber(), modeContex, line);
                                
                                self = selfRef;
                                break;
                            }
                    }


                }
                catch (Exception e)
                {
                    Log.Console(e.ToString());
                }
            }
        }
    }
}