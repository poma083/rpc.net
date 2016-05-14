using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using rpcTestCommon;

namespace testServer.TestType
{
    public static class UITest
    {
        static UserInfo ui;

        static UITest()
        {
            ui = new UserInfo()
            {
                Address = "СССР",
                Family = "Гагарин",
                Father = "Алексеевич",
                DateCreate = DateTime.Now,
                LastDateUpdate = DateTime.Now,
                Name = "Юрий",
                Phone = Guid.NewGuid().ToString("N")
            };
        }

        public static UserInfo InvokeMethod_UserInfoAnswer(UserInfo ui)
        {
            return ui;
        }
    }
    public class TestType : InvokeInterface
    {
        #region ITestType Members
        static byte[] ttt = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 0xa, 0xb, 0xc, 0xd, 0xe, 0xf };
        //static UserInfo ui;

        public int TestSumm(int a1, int a2)
        {
            return a1 + a2;
        }
        public void TestVoid()
        {
        }
        public DateTime? TestNulable(DateTime arg)
        {
            return null;
        }
        public DateTime TestException()
        {
            throw new NotImplementedException();
        }

        public TestClass TestBufferResize(TestClass tc)
        {
            //byte[] archiveContent = null;
            //try
            //{
            //    using (FileStream fs = new FileStream("serverTest.exe", FileMode.Open, FileAccess.Read, FileShare.Read))
            //    {
            //        archiveContent = new byte[fs.Length];
            //        fs.Read(archiveContent, 0, archiveContent.Length);
            //    }
            //}
            //catch(Exception e){

            //}
            TestClass result = new TestClass()
            {
                Content = ttt//archiveContent//
            };
            return result;
        }
        public DateTime InvokeMethod_UserInfo(UserInfo ui)
        {
            return DateTime.Now;
        }
        public UserInfo InvokeMethod_UserInfoAnswer(UserInfo ui)
        {
            UserInfo result = new UserInfo()
            {
                Address = ui.Address,
                Family = ui.Family,
                Father = ui.Father,
                Name = ui.Name,
                DateCreate = ui.DateCreate,
                Phone = "answer"
            };
            result.LastDateUpdate = DateTime.Now;
                
            return result;
        }
        #endregion
    }
}
