using System;
using System.Text.RegularExpressions;

namespace ET
{
    [Invoke]
    public class LogInvoker_Unity: AInvokeHandler<LogInvoker, ILog>
    {
        public override ILog Handle(LogInvoker args)
        {
            return new UnityLogger(args.SceneName);
        }
    }
    
    public class UnityLogger: ILog
    {
        private readonly string sceneName;

        public UnityLogger(string sceneName)
        {
            this.sceneName = sceneName;
        }
        
        public void Trace(string msg)
        {
            UnityEngine.Debug.Log($"{this.sceneName} {msg}");
        }

        public void Debug(string msg)
        {
            UnityEngine.Debug.Log($"{this.sceneName} {msg}");
        }

        public void Info(string msg)
        {
            UnityEngine.Debug.Log($"{this.sceneName} {msg}");
        }

        public void Warning(string msg)
        {
            UnityEngine.Debug.LogWarning($"{this.sceneName} {msg}");
        }

        public void Error(string msg)
        {
#if UNITY_EDITOR
            msg = Msg2LinkStackMsg(msg);
#endif
            UnityEngine.Debug.LogError(msg);
        }
        
        private static string Msg2LinkStackMsg(string msg)
        {
            msg = Regex.Replace(msg,@"at (.*?) in (.*?\.cs):(\w+)", match =>
            {
                string path = match.Groups[2].Value;
                string line = match.Groups[3].Value;
                return $"{match.Groups[1].Value}\n<a href=\"{path}\" line=\"{line}\">{path}:{line}</a>";
            });
            return msg;
        }

        public void Error(Exception e)
        {
            UnityEngine.Debug.LogException(e);
        }
    }
}