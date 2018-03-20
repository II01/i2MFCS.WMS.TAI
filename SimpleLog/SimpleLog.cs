using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace SimpleLogs
{

    public class SimpleLog
    {

        public enum Severity { EVENT, EXCEPTION }
        private string _fileName { get; set; }
        private object _lockFile = new object();
        static private object _lockSingleton = new object();
        static private SimpleLog _singleton;
        private bool _on = false;

        public SimpleLog()
        {
            _on = false;
            try
            {
                _fileName = ConfigurationManager.AppSettings["txtlog"];
                _on = Convert.ToBoolean(ConfigurationManager.AppSettings["logtofile"]);
            }
            catch
            { }
        }

        public static SimpleLog Singleton
        {
            get
            {
                if (_singleton == null)
                    lock (_lockSingleton)
                        if (_singleton == null)
                            _singleton = new SimpleLog();
                return _singleton;
            }
        }

        static public void AddException(Exception ex, string device, [CallerMemberName] string member = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int line = 0)
        {
            string[] str = fileName.Split('\\');
            Singleton.WriteLog(Severity.EXCEPTION, device, $"{member} ({str[str.Length - 1]} {line})", ex.Message);
        }

        static public void AddLog(Severity severity, string device, string message, string module)
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
                    sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss:fff}->{severity}:{device??"null"}:{message??"null"}:{module??"null"}");
                    sw.WriteLine();
                }
            }
        }

    }
}
