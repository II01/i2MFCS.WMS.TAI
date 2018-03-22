using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.CompilerServices;

namespace i2MFCS.WMS.Database.Tables
{
    public class Log
    {
        public enum SeverityEnum { Exception, Event }
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int ID { get; set; }
        [Required]
        public SeverityEnum Severity { get; set; }
        [Required, MaxLength(250)]
        public string Message { get; set; }
        [Required, MaxLength(250)]
        public string Source { get; set; }
        [Required, DatabaseGenerated(DatabaseGeneratedOption.Computed)]
        public DateTime Time { get; set; }

        static public void AddException(Exception ex, [CallerMemberName] string member = "", [CallerFilePath] string fileName = "", [CallerLineNumber] int line = 0)
        {
            try
            {
                string[] str = fileName.Split('\\');
                using (var dc = new WMSContext())
                {
                    dc.Logs.Add(new Log
                    {
                        Severity = SeverityEnum.Exception,
                        Message = ex.Message.Substring(0, Math.Min(250, ex.Message.Length)),
                        Source = $"{member} ({str[str.Length - 1]} {line})"
                    });
                    dc.SaveChanges();
                }
            }
            catch { }
        }

        static public void AddLog(SeverityEnum severity, string source, string message)
        {
            try
            {
                using (var dc = new WMSContext())
                {
                    dc.Logs.Add(new Log
                    {
                        Severity = SeverityEnum.Exception,
                        Message = message.Substring(0, Math.Min(250, message.Length)),
                        Source = source
                    });
                    dc.SaveChanges();
                }
            }
            catch { }
        }
    }
}
