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
            Console.BufferWidth = 999;
            Console.BufferHeight = 9999;

            PDUConfigSection pduConfig = PDUConfigSection.GetConfig();
            Client client = new Client(pduConfig.Clients["local"]);

            client.OnBindTransceiverCompleeted += BindTransceiverResult;
            client.OnInvokeCompleeted += InvokeResult;
            //client.OnInvokeCompleeted += InvokeResult_empty;

            Assembly invokeAssembly = Assembly.GetAssembly(typeof(InvokeInterface));
            Type invokeType = invokeAssembly.GetTypes().Where(t => t.Name == "InvokeInterface").SingleOrDefault();
            BindingFlags bf = BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
            MethodInfo invokeMethod = invokeType.GetMethod("InvokeMethod_UserInfo", bf);
            MethodInfo invokeMethodNull = invokeType.GetMethod("InvokeMethod_ReturnNull", bf);
            MethodInfo invokeMethodEmptyResult = invokeType.GetMethod("InvokeMethod_EmptyResult", bf);

            client.Connect(pduConfig.Clients["local"].Login, pduConfig.Clients["local"].Password);

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

                PDUInvokeByName pi = client.CreateInvokeByName("byName.InvokeMethod_UserInfo", new object[] { ui });
                client.InvokeAsync(pi);
                PDUInvokeByName piNull = client.CreateInvokeByName("byName.InvokeMethod_ReturnNull", new object[] { });
                client.InvokeAsync(piNull);
                PDUInvokeByName piEmpty = client.CreateInvokeByName("byName.InvokeMethod_EmptyResult", new object[] { ui });
                client.InvokeAsync(piEmpty);

                current = DateTime.Now;
            }
            Console.ReadLine();
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

                PDUInvoke pi = client.CreateInvokeData(invokeAssembly, invokeType, invokeMethod, new object[] { ui });
                client.InvokeAsync(pi);
                PDUInvoke piNull = client.CreateInvokeData(invokeAssembly, invokeType, invokeMethodNull, new object[] { });
                client.InvokeAsync(piNull);
                PDUInvoke piEmpty = client.CreateInvokeData(invokeAssembly, invokeType, invokeMethodEmptyResult, new object[] { ui });
                client.InvokeAsync(piEmpty);

                current = DateTime.Now;
            }
            Console.ReadLine();
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
            if (result.TypeName == typeof(DateTime?).FullName)
            {
                DateTime? dateTime = result.GetInvokeResult<DateTime?>();
                Console.WriteLine(String.Format("type: {0} ; item: {1} ; Date: {2}", result.CommandID, result.Sequence, dateTime != null ? ((DateTime)dateTime).ToString("yyyy-MM-dd HH:mm:ss:fff") : "null"));
            }
            else if (result.TypeName == typeof(DateTime).FullName)
            {
                DateTime dateTime = result.GetInvokeResult<DateTime>();
                Console.WriteLine(String.Format("type: {0} ; item: {1} ; Date: {2}", result.CommandID, result.Sequence, dateTime.ToString("yyyy-MM-dd HH:mm:ss:fff")));
            }
            else if (result.TypeName == typeof(void).FullName)
            {
                Console.WriteLine(String.Format("type: {0} ; item: {1} ; Date: {2}", result.CommandID, result.Sequence, "void"));
            }
        }
    }
}
