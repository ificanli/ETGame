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

        [Option('s', "SceneName", Required = true, HelpText = "场景名称，如 WOW 对应 cn.etetet.wow")]
        public string SceneName { get; set; }
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

            if (string.IsNullOrEmpty(options.SceneName))
            {
                throw new Exception("SceneName 不能为空，请在 GlobalConfig 中配置 SceneName");
            }

            CodeModeChangeHelper.ChangeToCodeMode(options.CodeMode.ToString(), options.SceneName);

            Console.WriteLine("change codemode ok!");
            return 0;
        }
    }
}