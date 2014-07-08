using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PDUServer;
using PDUDatas;
using rpcTestCommon;

namespace serverTest
{
    class Program : InvokeInterface
    {
        static void Main(string[] args)
        {
            ResultServerConfig cfg = new ResultServerConfig()
            {
                Host = "0.0.0.0",
                Port = 3737
            };
            Server server = new Server(cfg);
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

        public DateTime InvokeMethod_UserInfo(UserInfo ui)
        {
            return DateTime.Now;
        }
        public DateTime? InvokeMethod_ReturnNull(UserInfo ui)
        {
            return null;
        }
        public void InvokeMethod_EmptyResult(UserInfo ui)
        {
            ui.LastDateUpdate = DateTime.Now;
        }
    }
}
