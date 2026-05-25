using System;

namespace LockStepLib.Core
{
    /// <summary>
    /// 日志级别
    /// </summary>
    public enum LogLevel
    {
        Debug = 0,
        Info = 1,
        Warn = 2,
        Error = 3,
        None = 99
    }

    /// <summary>
    /// 日志抽象层。通过注入外部 Action 适配不同环境 (Unity Debug.Log / Console.WriteLine)。
    /// 帧同步核心代码不直接依赖 UnityEngine。
    /// </summary>
    public static class LogManager
    {
        private static Action<string> _debugLogger;
        private static Action<string> _infoLogger;
        private static Action<string> _warnLogger;
        private static Action<string> _errorLogger;

        private static LogLevel _minLevel = LogLevel.Debug;

        /// <summary>当前最低输出级别</summary>
        public static LogLevel MinLevel
        {
            get => _minLevel;
            set => _minLevel = value;
        }

        /// <summary>
        /// 注入外部日志实现。
        /// debug/info 通常指向 Debug.Log，warn 指向 Debug.LogWarning，error 指向 Debug.LogError。
        /// </summary>
        public static void Initialize(
            Action<string> debugLogger,
            Action<string> infoLogger,
            Action<string> warnLogger,
            Action<string> errorLogger)
        {
            _debugLogger = debugLogger;
            _infoLogger = infoLogger;
            _warnLogger = warnLogger;
            _errorLogger = errorLogger;
        }

        /// <summary>便捷初始化，全部使用默认 Console.WriteLine</summary>
        public static void InitializeConsole()
        {
            Initialize(
                msg => Console.WriteLine($"[DBG] {msg}"),
                msg => Console.WriteLine($"[INF] {msg}"),
                msg => Console.WriteLine($"[WRN] {msg}"),
                msg => Console.WriteLine($"[ERR] {msg}")
            );
        }

        public static void Debug(string msg)
        {
            if (_minLevel <= LogLevel.Debug)
                _debugLogger?.Invoke(msg);
        }

        public static void Info(string msg)
        {
            if (_minLevel <= LogLevel.Info)
                _infoLogger?.Invoke(msg);
        }

        public static void Warn(string msg)
        {
            if (_minLevel <= LogLevel.Warn)
                _warnLogger?.Invoke(msg);
        }

        public static void Error(string msg)
        {
            if (_minLevel <= LogLevel.Error)
                _errorLogger?.Invoke(msg);
        }
    }
}
