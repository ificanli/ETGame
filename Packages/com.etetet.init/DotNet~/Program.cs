using System;
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
            
            CodeModeChangeHelper.ChangeToCodeMode(options.CodeMode.ToString());
            
            Console.WriteLine("change codemode ok!");
            return 0;
        }
    }
}