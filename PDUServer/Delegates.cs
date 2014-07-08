using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PDUDatas;

namespace PDUServer
{
    public delegate void BeforeEventHandler(PDU data, ConnectionInfo ci);
    public delegate void AffterEventHandler(PDU data, ConnectionInfo ci);
}
