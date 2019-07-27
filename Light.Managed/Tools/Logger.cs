using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Light.Managed.Tools
{
    class Logger
    {
#if DEBUG
        private static readonly Severity _expectedSeverity = Severity.Debug;
#else
        private static readonly Severity _expectedSeverity = Severity.Error;
#endif

        public static readonly List<string> LogData = new List<string>();

        public static void PrintLog(Severity severity, string channel, string content)
        {
            if (_expectedSeverity < severity) return;
#if DEBUG
            Debug.WriteLine($"{DateTime.Now.ToString("O")} [{severity}] {channel}: {content}");
#endif
            LogData.Add($"{DateTime.Now.ToString("O")} [{severity}] {channel}: {content}");
        }
    }

    enum Severity
    {
        Info = 0,
        Log = 1,
        Warning = 2,
        Error = 3,
        Fatal = 4,
        Debug = 5
    }

    static class LogTemplate
    {
        public static string DbBeginTranscation = "Transcation started in {0} ms.";
        public static string DbCompleteTranscation = "Transcation completed in {0} ms.";
        public static string DbConstruction = "Class construction and database connection completed in {0} ms.";
        public static string DbEndLifetime = "Database writer was disposed.";
        public static string DbEndConn = "Database connection was ended in {0} ms.";
    }

    class PerfCounter
    {
        private readonly DateTime _startDateTime;

        public PerfCounter()
        {
            _startDateTime = DateTime.Now;
        }

        public TimeSpan FinalizeCounter() => (DateTime.Now - _startDateTime);

    }
}
