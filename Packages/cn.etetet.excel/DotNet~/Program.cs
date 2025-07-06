using System;
using CommandLine;

namespace ET
{
    internal static class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
                {
                    Log.Console(e.ExceptionObject.ToString());
                };
                
                // 命令行参数
                Parser.Default.ParseArguments<Options>(Environment.GetCommandLineArgs())
                        .WithNotParsed(error => throw new Exception($"命令行格式错误! {error}"))
                        .WithParsed((o)=>World.Instance.AddSingleton(o));
                Options.Instance.Console = 1;
                World.Instance.AddSingleton<Logger>().Log = new NLogger("ExcelExporter", 1, 0);
                
                
                foreach (Type type in typeof(Program).Assembly.GetTypes())
                {
                    if (!typeof(IExcelHandler).IsAssignableFrom(type))
                    {
                        continue;
                    }

                    if (type == typeof(IExcelHandler))
                    {
                        continue;
                    }
                    
                    IExcelHandler iExcelHandler = Activator.CreateInstance(type) as IExcelHandler;
                    iExcelHandler.Run();
                }
                
                LubanGen.CreateLubanConf();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                return -1;
            }
            Console.WriteLine("excelexporter ok!");
            return 0;
        }
    }
}