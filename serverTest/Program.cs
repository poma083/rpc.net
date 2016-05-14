using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PDUServer;
using PDUDatas;
using rpcTestCommon;

namespace serverTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.BufferHeight = 9999;
            Console.BufferWidth = 999;
            
            PDUConfigSection pduConfig = PDUConfigSection.GetConfig();
            Server server = new Server(pduConfig.Server);
            server.evConnect += beforeConnect;
            server.evInvoke += beforeInvoke;
            server.Start();

            System.Threading.Thread.Sleep(1203981);
        }

        static void beforeConnect(PDU sender, ConnectionInfo ci)
        {
            string ttt = ((PDUBindTransceiver)sender).SystemID;
        }
        static void beforeInvoke(PDU data, ConnectionInfo ci)
        {
            Type ttt = ((PDUInvoke)data).InstanceType;
        }
    }
}
