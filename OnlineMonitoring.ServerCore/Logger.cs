using System;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace OnlineMonitoring.ServerCore
{
    public class Logger
    {
        public Logger(EventLog eventLog, string logPath)
        {
            EventLog = eventLog;
            LoggerPath = Path.Combine(Directory.GetCurrentDirectory(), logPath);
            if (!Directory.Exists(LoggerPath))
            {
                Directory.CreateDirectory(LoggerPath);
            }
        }

        private readonly object _thisLock = new object();

        public EventLog EventLog { get; private set; }
        public string LoggerPath { get; set; }

        public void ErrorWriteLine(Exception exception)
        {
            ErrorWriteLine(string.Format("Exception: {0}; InnerException: {1}", exception, exception.InnerException != null ? exception.InnerException.Message : ""));
        }
        public void ErrorWriteLine(string message)
        {
            var log = GetLog(message);
            if (EventLog != null)
            {
                EventLog.WriteEntry(log, EventLogEntryType.Error);
            }
            lock (_thisLock)
            {
                File.AppendAllText(Path.Combine(LoggerPath, "retranslator-error.log"), log, Encoding.Default);
            }
        }

        public void WarningWriteLine(Exception exception)
        {
            WarningWriteLine(string.Format("Exception: {0}; InnerException: {1}", exception, exception.InnerException != null ? exception.InnerException.Message : ""));
        }
        public void WarningWriteLine(string message)
        {
            var log = GetLog(message);
            if (EventLog != null)
            {
                EventLog.WriteEntry(log, EventLogEntryType.Warning);
            }
            lock (_thisLock)
            {
                File.AppendAllText(Path.Combine(LoggerPath, "retranslator-warning.log"), log, Encoding.Default);
            }
        }

        public void MessageWriteLine(string message)
        {
            var log = GetLog(message);
            if (EventLog != null)
            {
                EventLog.WriteEntry(log, EventLogEntryType.Information);
            }
            lock (_thisLock)
            {
                File.AppendAllText(Path.Combine(LoggerPath, "retranslator-message.log"), log, Encoding.Default);
            }
        }

        public void PacketWriteLine(string message)
        {
            var log = GetLog(message);
            var currentFile = Path.Combine(LoggerPath, string.Format("packets-{0:yyyy-MM-dd}.log", DateTime.Now));
            lock (_thisLock)
            {
                File.AppendAllText(currentFile, log, Encoding.Default);
            }
        }

        public void CommandWriteLine(string message)
        {
            var log = GetLog(message);
            var currentFile = Path.Combine(LoggerPath, string.Format("commands-{0:yyyy-MM-dd}.log", DateTime.Now));
            lock (_thisLock)
            {
                File.AppendAllText(currentFile, log, Encoding.Default);
            }
        }

        private static string GetLog(string message)
        {
            return string.Format("{0:dd.MM.yy HH:mm:ss} {1}{2}{2}", DateTime.Now, message, Environment.NewLine);
        } 
    }
}