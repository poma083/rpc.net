using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using PDUClient;
using PDUDatas;
using rpcTestCommon;

namespace ClientTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Client client = new Client("127.0.0.1", 3737, 10000);

            client.OnBindTransceiverCompleeted += BindTransceiverResult;
            client.OnInvokeCompleeted += InvokeResult;
            //client.OnInvokeCompleeted += InvokeResult_empty;

            Assembly invokeAssembly = Assembly.GetAssembly(typeof(InvokeInterface));
            Type invokeType = invokeAssembly.GetTypes().Where(t => t.Name == "InvokeInterface").SingleOrDefault();
            BindingFlags bf = BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
            MethodInfo invokeMethod = invokeType.GetMethod("InvokeMethod_UserInfo", bf);
            //MethodInfo invokeMethod = invokeType.GetMethod("InvokeMethod_ReturnNull", bf);
            //MethodInfo invokeMethod = invokeType.GetMethod("InvokeMethod_EmptyResult", bf);

            client.Connect("sdfsdf", "sdfsdfsdf");

            DateTime start = DateTime.Now;
            DateTime current = DateTime.Now;
            for (int i = 0; i < 100; i++)
            {
                UserInfo ui = new UserInfo()
                {
                    Address = "Address",
                    Name = "Name",
                    Family = "Family",
                    Father = "Father",
                    Phone = "78658678",
                    LastDateUpdate = DateTime.Now
                };

                client.Invoke(invokeAssembly, invokeType, invokeMethod, new object[] { ui });
                //System.Threading.Thread.Sleep(0);

                current = DateTime.Now;
            }
            System.Threading.Thread.Sleep(3487623);
        }

        static void BindTransceiverResult(PDUBindTransceiverResp result)
        {
            Console.WriteLine(String.Format("type: {0} ; item: {1} ; Date: {2}", result.CommandID, result.Sequence, result.SystemID));
        }
        static void InvokeResult_empty(PDUInvokeResp result)
        {
            DateTime? dateTime = result.GetInvokeResult<DateTime?>();
            Console.WriteLine(String.Format("type: {0} ; item: {1} ", result.CommandID, result.Sequence));
        }
        static void InvokeResult(PDUInvokeResp result)
        {
            DateTime? dateTime = result.GetInvokeResult<DateTime?>();
            Console.WriteLine(String.Format("type: {0} ; item: {1} ; Date: {2}", result.CommandID, result.Sequence, dateTime != null ? ((DateTime)dateTime).ToString("yyyy-MM-dd HH:mm:ss:fff") : "null"));
        }
    }
}
