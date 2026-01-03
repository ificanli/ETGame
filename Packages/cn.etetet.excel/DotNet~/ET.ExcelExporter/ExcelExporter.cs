using System.Collections.Generic;
using System.IO;
using OfficeOpenXml;

namespace ET
{
    public static class ExcelExporter
    {
        private static Dictionary<string, ExcelPackage> packages = new();


        public static ExcelPackage GetPackage(string filePath)
        {
            if (!packages.TryGetValue(filePath, out var package))
            {
                using Stream stream = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
                package = new ExcelPackage(stream);
                packages[filePath] = package;
            }

            return package;
        }
    }
}
