using System;

namespace ET
{
    [EnableClass]
    internal static class Init
    {
        private static int Main(string[] args)
        {
            try
            {
                //NoCut.Run();

                //ExcelExporter.Export();

                foreach (Type type in typeof(Init).Assembly.GetTypes())
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
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            Console.WriteLine("excelexporter ok!");
            return 1;
        }
    }
}