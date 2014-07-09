using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDUServer
{
    public class ResultServerConfig
    {

        public string Host { get; set; }
        public UInt16 Port { get; set; }
        public UInt32 EnquireLinkPeriod { get; set; }
    }
}
