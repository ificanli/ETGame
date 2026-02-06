using System;
using System.Collections.Generic;
using System.IO;
using CommandLine;

namespace ET
{
    enum CodeMode
    {
        Client,
        Server,
        ClientServer,
    }

    class Options
    {
        [Option('c', "CodeMode", Required = false, Default = CodeMode.ClientServer)]
        public CodeMode CodeMode { get; set; }
    }

    internal static class Program
    {
        private static int Main(string[] args)
        {
            Options options = null;
            // 命令行参数
            Parser.Default.ParseArguments<Options>(Environment.GetCommandLineArgs())
                    .WithNotParsed(error => throw new Exception($"命令行格式错误! {error}"))
                    .WithParsed((o) => options = o);

            string mainPackagePath = Path.Combine("./", "MainPackage.txt");

            if (!File.Exists(mainPackagePath))
            {
                Console.WriteLine("MainPackage.txt not found, skip change codemode.");
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
                Console.WriteLine("MainPackage.txt is empty, skip change codemode.");
                return 0;
            }

            CodeModeChangeHelper.ChangeToCodeMode(options.CodeMode.ToString(), packages);

            Console.WriteLine("change codemode ok!");
            return 0;
        }
    }
}