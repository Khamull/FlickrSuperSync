using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FlickrSuperSyncConsole.Log
{
    public class Logger
    {
        private static readonly log4net.ILog log =
           log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        public static void Info(string info)
        {
            log.Info(info);
        }

        public static void Error(Exception ex)
        {
            log.Error(ex.Message, ex);
        }

        public static void Error(string message, Exception ex)
        {
            log.Error(message, ex);
        }
    }
}
