using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SimpleLog
{

    public class Log
    {

        public enum Severity { EVENT, EXCEPTION }
        private string _fileName { get; set; }
        private object _lockFile;
        private object _lockSingleton;
        private static Log _singleton;

        private bool _on { get; set; }

        public Log()
        {
            _lockFile = new object();
            _lockSingleton = new object();
            _on = false;
            try
            {
                _fileName = ConfigurationManager.AppSettings["txtlog"];
                _on = Convert.ToBoolean(ConfigurationManager.AppSettings["logtofile"]);
            }
            catch
            { }
        }

        public static Log Singleton
        {
            get
            {
                if (_singleton == null)
                    lock (_singleton)
                        if (_singleton == null)
                            _singleton = new Log();
                return _singleton;
            }
        }

        public static void AddException(Exception ex, string device, [CallerMemberName] string member = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int line = 0)
        {
            string[] str = fileName.Split('\\');
            Singleton.WriteLog(Severity.EXCEPTION, device, $"{member} ({str[str.Length - 1]} {line})", ex.Message);
        }

        public static void AddLog(Severity severity, string device, string message, string module)
        {
            Singleton.WriteLog(severity, device, message, module);
        }

        public void WriteLog(Severity severity, string device, string message, string module)
        {

            if (!_on)
                return;

            lock (_lockFile)
            {
                using (StreamWriter sw = new StreamWriter(String.Format(_fileName, DateTime.Now), true))
                {
                    sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff}->{severity}:{device}:{message}:{module}");
                    sw.WriteLine();
                }
            }
        }

    }
}
