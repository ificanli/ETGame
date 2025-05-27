#if DOTNET
using System;
using System.IO;
using System.Runtime.CompilerServices;
using NLog;

namespace ET
{
    [Invoke]
    public class LogInvoker_NLog: AInvokeHandler<LogInvoker, ILog>
    {
        public override ILog Handle(LogInvoker args)
        {
            return new NLogger(args.SceneName, args.Process, args.Fiber);
        }
    }

    public class NLogger: ILog
    {
        private readonly NLog.Logger logger;
        
        private readonly string sceneName;

        static NLogger()
        {
            LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration("Packages/cn.etetet.loader/Scripts/Loader/Server/NLog.config");
            LogManager.Configuration.Variables["currentDir"] = Environment.CurrentDirectory;
        }

        public NLogger(string name, int process, int fiber)
        {
            this.sceneName = name;
            this.logger = LogManager.GetLogger($"{(uint)process:000000}.{(uint)fiber:0000000000}.{name}");
        }

        public void Trace(string message)
        {
            this.logger.Trace($"{this.sceneName} {message}");
        }

        public void Warning(string message)
        {
            this.logger.Warn($"{this.sceneName} {message}");
        }

        public void Info(string message)
        {
            this.logger.Info($"{this.sceneName} {message}");
        }

        public void Debug(string message)
        {
            this.logger.Debug($"{this.sceneName} {message}");
        }

        public void Error(string message)
        {
            this.logger.Error($"{this.sceneName} {message}");
        }

        public void Error(Exception e)
        {
            this.logger.Error($"{this.sceneName} {e}");
        }

        public void Trace(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message)
        {
            this.logger.Trace($"{this.sceneName} {message.ToStringAndClear()}");
        }

        public void Warning(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message)
        {
            this.logger.Warn($"{this.sceneName} {message.ToStringAndClear()}");
        }

        public void Info(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message)
        {
            this.logger.Info($"{this.sceneName} {message.ToStringAndClear()}");
        }

        public void Debug(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message)
        {
            this.logger.Debug($"{this.sceneName} {message.ToStringAndClear()}");
        }

        public void Error(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message)
        {
            this.logger.Error($"{this.sceneName} {message.ToStringAndClear()}");
        }
    }
}
#endif