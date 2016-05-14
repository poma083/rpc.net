using PDUClient;
using PDUDatas;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WaitTestClient
{
    class Program
    {
        static Client pduClient;
        static string methodNameForWait = "byName.TestSummMethod";
        static ManualResetEvent rndTimeWait = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            Console.BufferHeight = 9999;
            Console.BufferWidth = 999;

            pduClient = Init();
            pduClient.OnBindTransceiverCompleeted += pduClient_OnBindTransceiverCompleeted;
            pduClient.OnInvokeCompleeted += pduClient_OnInvokeCompleeted;
            pduClient.Connect();

            pduClient.WaitInvoke(methodNameForWait, WaitCallback);

            Random rnd = new Random(DateTime.Now.Millisecond);
            for (; ; )
            {
                int milliseccondsToWait = rnd.Next(32768);
                rndTimeWait.WaitOne(milliseccondsToWait);
                PDUInvokeByName ibn = pduClient.CreateInvokeByName(methodNameForWait, new object[] { 1, 5 });
                try
                {
                    Logger.Log.Info(string.Format("Invoke: sequence=\"{0}\" byName=\"{1}\"", ibn.Sequence, methodNameForWait));
                    pduClient.InvokeAsync(ibn, InvokeCallback);
                }
                catch (PDURequestException pre)
                {
                    Logger.Log.Info(pre.InnerException);
                }
            
            }
        }

        static Client Init()
        {
            Client result = null;
            PDUConfigSection sec = PDUConfigSection.GetConfig();
            ClientCfgClass clientCfg = sec.Clients["local"];
            if (clientCfg != null)
            {
                result = new Client(clientCfg);
            }
            return result;
        }

        static void pduClient_OnInvokeCompleeted(PDUResp response)
        {
            string typeName;
            object data;
            response.GetData(out typeName, out data);
            Logger.Log.InfoFormat("OnInvokeCompleeted sequence=\"{0}\" typeName=\"{1}\"", response.Sequence, typeName);
        }
        static void WaitCallback(PDUWaitResp pduResponse)
        {
            uint seq = pduResponse.Sequence;
            Logger.Log.Info(string.Format("Wait result sequence=\"{0}\"", seq));
            pduClient.WaitInvoke(methodNameForWait, WaitCallback);
        }
        static void InvokeCallback(PDUResp pduResponse)
        {
            //int summ = pduResponse.GetInvokeResult<int>();
            Logger.Log.Info(string.Format("InvokeCallback: sequence=\"{0}\"", pduResponse.Sequence));
        }
        static void pduClient_OnBindTransceiverCompleeted(PDUBindTransceiverResp response)
        {
            if (response.CommandState != 0)
            {
                Logger.Log.Info("Неудача авторизации");
            }
        }
    }
}
