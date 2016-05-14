using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using PDUClient;
using System.Reflection;
using System.Configuration;
using PDUDatas;
using System.Net.Sockets;
using System.IO;
using rpcTestCommon;
using System.Security.Cryptography.X509Certificates;
using System.Runtime.InteropServices;

namespace testClient
{
    class Program
    {
        static Client PDUClient;

        static Assembly invokeAssembly = null;
        static Type invokeType = null;

        static MethodInfo invokeMethodSumm = null;
        static MethodInfo invokeMethodVoid = null;
        static MethodInfo invokeMethodNullable = null;
        static MethodInfo invokeMethodException = null;
        static MethodInfo invokeMethodTestBufferResize = null;
        static MethodInfo invokeMethod_UserInfo = null;

        static StringBuilder sb = new StringBuilder();

        static void Main(string[] args)
        {
            Console.BufferHeight = 9999;
            Console.BufferWidth = 999;

            PDUConfigSection sec = PDUConfigSection.GetConfig();
            ClientCfgClass clientCfg = sec.Clients["local"];
            if (clientCfg != null)
            {
                PDUClient = new Client(clientCfg);
            }
            PDUClient.Connect();
            PDUClient.OnBindTransceiverCompleeted += BindTransceiverResult;
            PDUClient.OnInvokeCompleeted += InvokeResult;
            PDUClient.OnEnquireLinkCompleeted += EnquireLink;

            Logger.Log.Info("///////////////////////////    test DateTime.Parse call with onew argument   ///////////////////////////");
            Logger.Log.Info("press entyer to continue");
            Console.ReadLine();
            #region test DateTime.Parse call with onew argument
            PDUInvokeByName ibn_dtParse = PDUClient.CreateInvokeByName("DateTime.Parse", new object[] { "2016-05-23 23:44:04" });
            try
            {
                Logger.Log.Info("Invoke.Before DateTime.Parse");
                DateTime dtParse = PDUClient.Invoke<DateTime>(ibn_dtParse);
                Logger.Log.Info(string.Format("{0}", dtParse));
                Logger.Log.Info("Invoke.Affter DateTime.Parse");
            }
            catch (PDURequestException pre)
            {
                Logger.Log.Info(pre.InnerException);
            }
            #endregion
            
            Logger.Log.Info("///////////////////////////    test sync call    ///////////////////////////");
            Logger.Log.Info("///////////////////////////   test call by name  ///////////////////////////");
            Logger.Log.Info("press entyer to continue");
            Console.ReadLine();
            int arg1 = 5;
            int arg2 = -4;
            #region TestSummMethod
            PDUInvokeByName ibn_Summ = PDUClient.CreateInvokeByName("byName.TestSummMethod", new object[] { arg1, arg2 });
            try
            {
                Logger.Log.Info("Invoke.Before TestSummMethod");
                int summ = PDUClient.Invoke<int>(ibn_Summ);
                Logger.Log.Info(string.Format("{0} + {1} = {2}", arg1, arg2, summ));
                Logger.Log.Info("Invoke.Affter TestSummMethod");
            }
            catch (PDURequestException pre)
            {
                Logger.Log.Info(pre.InnerException);
            }
            #endregion
            #region test wait
            //for (; ; )
            //{
            //    PDUClient.WaitInvoke("byName.TestSummMethod", WaitCallback);
            //}
            #endregion
            #region TestExceptionMethod
            PDUInvokeByName ibn_Exception = PDUClient.CreateInvokeByName("byName.TestExceptionMethod", new object[] { });
            try
            {
                Logger.Log.Info("Invoke.Before TestExceptionMethod");
                DateTime dt = PDUClient.Invoke<DateTime>(ibn_Exception);
                Logger.Log.Info("Invoke.Affter TestExceptionMethod");
            }
            catch (PDURequestException pre)
            {
                Logger.Log.Info(pre.InnerException);
            }
            #endregion
            #region TestVoidMethod
            PDUInvokeByName ibn_Void = PDUClient.CreateInvokeByName("byName.TestVoidMethod", new object[] { });
            try
            {
                Logger.Log.Info("Invoke.Before TestVoidMethod");
                object o = PDUClient.Invoke<object>(ibn_Void);
                Logger.Log.Info(string.Format("Void.Result = \"{0}\"", o));
                Logger.Log.Info("Invoke.Affter TestVoidMethod");
            }
            catch (PDURequestException pre)
            {
                Logger.Log.Info(pre.InnerException);
            }
            #endregion
            #region TestNulableMethod
            PDUInvokeByName ibn_Nullable = PDUClient.CreateInvokeByName("byName.TestNulableMethod", new object[] { DateTime.Now });
            try
            {
                Logger.Log.Info("Invoke.Before TestNulableMethod");
                DateTime? dt = PDUClient.Invoke<DateTime?>(ibn_Nullable);
                Logger.Log.Info(string.Format("Nullable.Result = \"{0}\"", dt));
                Logger.Log.Info("Invoke.Affter TestNulableMethod");
            }
            catch (PDURequestException pre)
            {
                Logger.Log.Info(pre.InnerException);
            }
            #endregion
            #region TestUserType
            UserInfo ui = new UserInfo()
            {
                Address = "СССР",
                Family = "Гагарин",
                Father = "Алексеевич",
                DateCreate = DateTime.Now,
                LastDateUpdate = DateTime.Now,
                Name = "Юрий",
                Phone = "-"
            };
            PDUInvokeByName ibn_UserType = PDUClient.CreateInvokeByName("TestUserType", new object[] { ui });
            try
            {
                Logger.Log.Info("Invoke.Before TestUserType");
                DateTime dt = PDUClient.Invoke<DateTime>(ibn_UserType);
                Logger.Log.Info(string.Format("TestUserType.Result = \"{0}\"", dt));
                Logger.Log.Info("Invoke.Affter TestUserType");
            }
            catch (PDURequestException pre)
            {
                Logger.Log.Info(pre.InnerException);
            }
            #endregion

            PDUClient.OnInvokeCompleeted -= InvokeResult;

            Logger.Log.Info("///////////////////////////    test sync call    ///////////////////////////");
            Logger.Log.Info("////////////////////////   test call by interface   ////////////////////////");
            Logger.Log.Info("press entyer to continue");
            Console.ReadLine();

            invokeAssembly = Assembly.GetAssembly(typeof(InvokeInterface)); // DataModel.SmartVista.ISmartVista
            invokeType = invokeAssembly.GetTypes().Where(t => t.Name == "InvokeInterface").SingleOrDefault();
            BindingFlags bf = BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;
            invokeMethodSumm = invokeType.GetMethod("TestSumm", bf);
            invokeMethodVoid = invokeType.GetMethod("TestVoid", bf);
            invokeMethodNullable = invokeType.GetMethod("TestNulable", bf);
            invokeMethodException = invokeType.GetMethod("TestException", bf);
            invokeMethod_UserInfo = invokeType.GetMethod("InvokeMethod_UserInfo", bf);

            #region TestSummMethod
            PDUInvoke i_Summ = PDUClient.CreateInvokeData(invokeAssembly, invokeType, invokeMethodSumm, new object[] { arg1, arg2 });
            try
            {
                Logger.Log.Info("Invoke.Before invokeMethodSumm");
                int summ = PDUClient.Invoke<int>(i_Summ);
                Logger.Log.Info(string.Format("{0} + {1} = {2}", arg1, arg2, summ));
                Logger.Log.Info("Invoke.Affter invokeMethodSumm");
            }
            catch (PDURequestException pre)
            {
                Logger.Log.Info(pre.InnerException);
            }
            #endregion
            #region TestExceptionMethod
            PDUInvoke i_Exception = PDUClient.CreateInvokeData(invokeAssembly, invokeType, invokeMethodException, new object[] { });
            try
            {
                Logger.Log.Info("Invoke.Before invokeMethodException");
                DateTime dt = PDUClient.Invoke<DateTime>(i_Exception);
                Logger.Log.Info("Invoke.Affter invokeMethodException");
            }
            catch (PDURequestException pre)
            {
                Logger.Log.Info(pre.InnerException);
            }
            #endregion
            #region TestVoidMethod
            PDUInvoke i_Void = PDUClient.CreateInvokeData(invokeAssembly, invokeType, invokeMethodVoid, new object[] { });
            try
            {
                Logger.Log.Info("Invoke.Before invokeMethodVoid");
                object o = PDUClient.Invoke<object>(i_Void);
                Logger.Log.Info(string.Format("Void.Result = \"{0}\"", o));
                Logger.Log.Info("Invoke.Affter invokeMethodVoid");
            }
            catch (PDURequestException pre)
            {
                Logger.Log.Info(pre.InnerException);
            }
            #endregion
            #region TestNulableMethod
            PDUInvoke i_Nullable = PDUClient.CreateInvokeData(invokeAssembly, invokeType, invokeMethodNullable, new object[] { DateTime.Now });
            try
            {
                Logger.Log.Info("Invoke.Before invokeMethodNullable");
                DateTime? dt = PDUClient.Invoke<DateTime?>(i_Nullable);
                Logger.Log.Info(string.Format("Nullable.Result = \"{0}\"", dt));
                Logger.Log.Info("Invoke.Affter invokeMethodNullable");
            }
            catch (PDURequestException pre)
            {
                Logger.Log.Info(pre.InnerException);
            }
            #endregion
            #region TestUserType
            PDUInvoke i_UserType = PDUClient.CreateInvokeData(invokeAssembly, invokeType, invokeMethod_UserInfo, new object[] { ui });
            try
            {
                Logger.Log.Info("Invoke.Before invokeMethodUserType");
                DateTime dt = PDUClient.Invoke<DateTime>(i_UserType);
                Logger.Log.Info(string.Format("TestUserType.Result = \"{0}\"", dt));
                Logger.Log.Info("Invoke.Affter invokeMethodUserType");
            }
            catch (PDURequestException pre)
            {
                Logger.Log.Info(pre.InnerException);
            }
            #endregion
            
            Logger.Log.Info("//////////////////////////    test async call     //////////////////////////");
            Logger.Log.Info("////////////////////////   test call by interface   ////////////////////////");
            Logger.Log.Info("press entyer to continue");
            Console.ReadLine();
            #region TestSummMethod
            try
            {
                Logger.Log.Info("Invoke.Before invokeMethodSumm");
                PDUClient.InvokeAsync(i_Summ, InvokeSummCallback);
                Logger.Log.Info("Invoke.Affter invokeMethodSumm");
            }
            catch (PDURequestException pre)
            {
                Logger.Log.Info(pre.InnerException);
            }
            #endregion
            #region TestExceptionMethod
            try
            {
                Logger.Log.Info("Invoke.Before invokeMethodException");
                PDUClient.InvokeAsync(i_Exception, InvokeExceptionCallback);
                Logger.Log.Info("Invoke.Affter invokeMethodException");
            }
            catch (PDURequestException pre)
            {
                Logger.Log.Info(pre.InnerException);
            }
            #endregion
            #region TestVoidMethod
            try
            {
                Logger.Log.Info("Invoke.Before invokeMethodVoid");
                PDUClient.InvokeAsync(i_Void, InvokeVoidCallback);
                Logger.Log.Info("Invoke.Affter invokeMethodVoid");
            }
            catch (PDURequestException pre)
            {
                Logger.Log.Info(pre.InnerException);
            }
            #endregion
            #region TestNulableMethod
            try
            {
                Logger.Log.Info("Invoke.Before invokeMethodNullable");
                PDUClient.InvokeAsync(i_Nullable, InvokeNullableCallback);
                Logger.Log.Info("Invoke.Affter invokeMethodNullable");
            }
            catch (PDURequestException pre)
            {
                Logger.Log.Info(pre.InnerException);
            }
            #endregion
            #region TestUserType
            try
            {
                Logger.Log.Info("Invoke.Before invokeMethodUserType");
                PDUClient.InvokeAsync(i_UserType, InvokeUserTypeCallback);
                Logger.Log.Info("Invoke.Affter invokeMethodUserType");
            }
            catch (PDURequestException pre)
            {
                Logger.Log.Info(pre.InnerException);
            }
            #endregion

            Logger.Log.Info("//////////////////////////    test Buffer Resize 1000 cycles     //////////////////////////");
            Logger.Log.Info("press entyer to continue");
            Console.ReadLine();
            #region testBufferResize
            byte[] archiveContent = null;
            try
            {
                using (FileStream fs = new FileStream("testClient.exe", FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    archiveContent = new byte[fs.Length];
                    fs.Read(archiveContent, 0, archiveContent.Length);
                }
            }
            catch (Exception ex)
            {

            }
            TestClass tc = new TestClass()
            {
                Content = archiveContent
            };
            Logger.Log.Info("testBufferResize start");
            for (int i = 0; i < 1000; i++)
            {
                //Console.ReadLine();
                //Logger.Log.InfoFormat("i={0}", i);
                //if((i % 100) == 0)
                //{
                //    Logger.Log.InfoFormat("i={0}", i);
                //}
                try
                {
                    PDUInvokeByName ibn_Test = PDUClient.CreateInvokeByName("byName.BufferResizeMethod", new object[] { tc });
                    //Logger.Log.Info("Invoke.Before BufferResizeMethod");
                    TestClass testClass = PDUClient.Invoke<TestClass>(ibn_Test);
                    if (testClass.Content != null)
                    {
                        //    Logger.Log.Info(string.Format("Length={0}", testClass.Content.Length));
                    }
                    else
                    {
                        Logger.Log.Info(string.Format("Length={0}", 0));
                    }

                    //Logger.Log.Info("Invoke.Affter BufferResizeMethod");
                }
                catch (PDURequestException pre)
                {
                    Logger.Log.Info(pre.InnerException);
                }
                catch (Exception ex)
                {
                    Logger.Log.Info(ex);
                }

            }
            #endregion

            Logger.Log.Info("//////////////////////////    test User Type transmit and receive 10 000 cycles     //////////////////////////");
            Logger.Log.Info("press entyer to continue");
            //int sss = Marshal.SizeOf(typeof(UserInfo));
            Console.ReadLine();
            #region TestUserType
            X509Certificate2 cs = SCZI.FindCertificate(clientCfg.ServerCertificate.StoreName,
                clientCfg.ServerCertificate.StoreLocation, clientCfg.ServerCertificate.Thumbprint);
            Logger.Log.Info("TestUserAnswerType start");
            for (int i = 0; i < 10000; i++)
            {
                UserInfo ui2 = new UserInfo()
                {
                    Address = "СССР",
                    Family = "Гагарин",
                    Father = "Алексеевич",
                    DateCreate = DateTime.Now,
                    LastDateUpdate = DateTime.Now,
                    Name = "Юрий",
                    Phone = Guid.NewGuid().ToString("N")
                };
                PDUInvokeByName ibn_UserType2 = PDUClient.CreateInvokeByName("TestUserAnswerType", new object[] { ui });
                //PDUInvokeSecureByName ibn_UserType2 = PDUClient.CreateInvokeSecureByName("TestUserAnswerType", new object[] { ui2 },
                //    clientCfg.ClientCertificate.StoreName, clientCfg.ClientCertificate.StoreLocation, clientCfg.ClientCertificate.Thumbprint,
                //    cs);
                try
                {
                    //Logger.Log.Info("Invoke.Before TestUserAnswerType");
                    PDUClient.InvokeAsync(ibn_UserType2, TestUserAnswerTypeCallback);
                    //Logger.Log.Info(string.Format("TestUserAnswerType.Result = \"{0}\"", u_answer.LastDateUpdate.ToString("HH:mm:ss.ttt")));
                    //Logger.Log.Info("Invoke.Affter TestUserAnswerType");
                }
                catch (PDURequestException pre)
                {
                    Logger.Log.Info(pre.InnerException);
                }
            }
            //Logger.Log.Info(sb.ToString());
            //sb.Clear();
            Console.ReadLine();
            Logger.Log.Info(sb.ToString());
            #endregion
            Logger.Log.Info("press entyer to exit");
            Console.ReadLine();
        }

        private static void TestUserAnswerTypeCallback(PDUResp pduResponse)
        {
            UserInfo uinf = pduResponse.GetInvokeResult<UserInfo>();
            sb.AppendLine(string.Format("TestUserAnswerTypeCallback time=\"{0}\" createTime=\"{1}\" result=\"{2}\"", DateTime.Now.ToString("HH:mm:ss.fff"), uinf.DateCreate.ToString("HH:mm:ss.fff"), uinf.LastDateUpdate.ToString("HH:mm:ss.fff")));
        }

        static void BindTransceiverResult(PDUBindTransceiverResp result)
        {
            if (result.CommandState != 0)
            {
                Logger.Log.Info("Неудача авторизации");
            }
        }
        static void InvokeResult(PDUResp result)
        {
            if (result.TypeName == typeof(Int32).FullName)
            {
                try
                {
                    Logger.Log.InfoFormat("{0}", result.GetInvokeResult<Int32>());
                }
                catch (PDURequestException ex)
                {
                    Logger.Log.Info(ex.InnerException);
                }
            }
            else if (result.TypeName == typeof(DateTime).FullName)
            {
                try
                {
                    Logger.Log.InfoFormat("{0}", result.GetInvokeResult<DateTime>());
                }
                catch (PDURequestException ex)
                {
                    Logger.Log.Info(ex.InnerException);
                }
            }
            else if (result.TypeName == typeof(DateTime?).FullName)
            {
                DateTime? res = result.GetInvokeResult<DateTime?>();
                if (res == null)
                {
                    Logger.Log.Info("null");
                }
                else
                {
                    Logger.Log.Info((DateTime)res);
                }
            }
            else if (result.TypeName == typeof(void).FullName)
            {
                Logger.Log.Info("void");
                try
                {
                    var ooo = result.GetInvokeResult<object>();
                }
                catch (PDURequestException ex)
                {
                    Logger.Log.Info(ex.InnerException);
                }
            }
            else if (result.TypeName == typeof(Exception).FullName)
            {
                var ooo = result.GetInvokeResult<Exception>();
            }
            else
            {
                Logger.Log.InfoFormat(String.Format("Тип возвращаемого значения \"{0}\" не задан", result.TypeName));
            }
        }
        static void EnquireLink(PDUEnquireLink link)
        {
            Logger.Log.Info(String.Format("receive EnquireLink sequence={0}", link.Sequence));
        }

        static void InvokeSummCallback(PDUResp pduResponse)
        {
            int summ = pduResponse.GetInvokeResult<int>();
            Logger.Log.Info(string.Format("InvokeSummCallback result=\"{0}\"", summ));
        }
        static void InvokeExceptionCallback(PDUResp pduResponse)
        {
            try
            {
                DateTime dt = pduResponse.GetInvokeResult<DateTime>();
            }
            catch (PDURequestException pre)
            {
                Logger.Log.Info(pre.InnerException);
            }
        }
        static void InvokeVoidCallback(PDUResp pduResponse)
        {
            object obj = pduResponse.GetInvokeResult<object>();
            Logger.Log.Info(string.Format("InvokeVoidCallback result=\"{0}\"", obj));
        }
        static void InvokeNullableCallback(PDUResp pduResponse)
        {
            DateTime? dt = pduResponse.GetInvokeResult<DateTime?>();
            Logger.Log.Info(string.Format("InvokeNullableCallback result=\"{0}\"", dt));
        }
        static void InvokeUserTypeCallback(PDUResp pduResponse)
        {
            DateTime dt = pduResponse.GetInvokeResult<DateTime>();
            Logger.Log.Info(string.Format("InvokeUserTypeCallback result=\"{0}\"", dt));
        }

        static void WaitCallback(PDUWaitResp pduResponse)
        {
            uint seq = pduResponse.Sequence;
            Logger.Log.Info(string.Format("Wait result sequence=\"{0}\"", seq));
        }
    }
}