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
            this.logger = LogManager.GetLogger($"{(uint)process:000000}.{(uint)fiber:000000}.{name}");
        }

        public void Trace(string message)
        {
            LogEventInfo logEvent = new(LogLevel.Trace, logger.Name, message);
            logEvent.Properties["sceneName"] = this.sceneName;
            logger.Log(logEvent);
        }

        public void Warning(string message)
        {
            LogEventInfo logEvent = new(LogLevel.Warn, logger.Name, message);
            logEvent.Properties["sceneName"] = this.sceneName;
            logger.Log(logEvent);
        }

        public void Info(string message)
        {
            LogEventInfo logEvent = new(LogLevel.Info, logger.Name, message);
            logEvent.Properties["sceneName"] = this.sceneName;
            logger.Log(logEvent);
        }

        public void Debug(string message)
        {
            LogEventInfo logEvent = new(LogLevel.Debug, logger.Name, message);
            logEvent.Properties["sceneName"] = this.sceneName;
            logger.Log(logEvent);
        }

        public void Error(string message)
        {
            LogEventInfo logEvent = new(LogLevel.Error, logger.Name, message);
            logEvent.Properties["sceneName"] = this.sceneName;
            logger.Log(logEvent);
        }

        public void Error(Exception e)
        {
            LogEventInfo logEvent = new(LogLevel.Info, logger.Name, e.ToString());
            logEvent.Properties["sceneName"] = this.sceneName;
            logger.Log(logEvent);
        }

        public void Trace(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message)
        {
            LogEventInfo logEvent = new(LogLevel.Trace, logger.Name, message.ToStringAndClear());
            logEvent.Properties["sceneName"] = this.sceneName;
            logger.Log(logEvent);
        }

        public void Warning(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message)
        {
            LogEventInfo logEvent = new(LogLevel.Warn, logger.Name, message.ToStringAndClear());
            logEvent.Properties["sceneName"] = this.sceneName;
            logger.Log(logEvent);
        }

        public void Info(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message)
        {
            LogEventInfo logEvent = new(LogLevel.Info, logger.Name, message.ToStringAndClear());
            logEvent.Properties["sceneName"] = this.sceneName;
            logger.Log(logEvent);
        }

        public void Debug(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message)
        {
            LogEventInfo logEvent = new(LogLevel.Debug, logger.Name, message.ToStringAndClear());
            logEvent.Properties["sceneName"] = this.sceneName;
            logger.Log(logEvent);
        }

        public void Error(ref System.Runtime.CompilerServices.DefaultInterpolatedStringHandler message)
        {
            LogEventInfo logEvent = new(LogLevel.Error, logger.Name, message.ToStringAndClear());
            logEvent.Properties["sceneName"] = this.sceneName;
            logger.Log(logEvent);
        }
    }
}
#endif