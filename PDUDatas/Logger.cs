using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using log4net;
using log4net.Config;

namespace PDUDatas
{
    public static class Logger
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(Logger));
        private static bool isInit = false;
        private static object sinkObject = new object();


        public static ILog Log
        {
            get
            {
                lock (sinkObject)
                {
                    if (!isInit)
                    {
                        InitLogger();
                    }
                }
                return log;
            }
        }

        private static void InitLogger()
        {
            XmlConfigurator.Configure();
            isInit = true;
        }
    }
}
