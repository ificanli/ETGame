using System;
using System.Collections.Generic;
using System.IO;

namespace ET
{
    internal static class Program
    {
        private static int Main()
        {
            try
            {
                string mainPackagePath = Path.Combine("./", "MainPackage.txt");

                if (!File.Exists(mainPackagePath))
                {
                    Console.WriteLine("MainPackage.txt not found, skip proto export.");
                    return 0;
                }

                string[] lines = File.ReadAllLines(mainPackagePath);
                HashSet<string> packages = new HashSet<string>();
                foreach (string line in lines)
                {
                    string pkg = line.Trim();
                    if (!string.IsNullOrWhiteSpace(pkg))
                    {
                        packages.Add(pkg);
                    }
                }

                if (packages.Count == 0)
                {
                    Console.WriteLine("MainPackage.txt is empty, skip proto export.");
                    return 0;
                }

                Proto2CS.Export(packages);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
            Console.WriteLine("proto2cs ok!");
            return 1;
        }
    }
}