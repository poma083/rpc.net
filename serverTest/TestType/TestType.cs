using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using rpcTestCommon;

namespace testServer.TestType
{
    public class TestType : InvokeInterface
    {
        #region ITestType Members

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
            byte[] archiveContent = null;
            try
            {
                using (FileStream fs = new FileStream("testServer.log", FileMode.Open, FileAccess.Read, FileShare.Read))
                {
                    archiveContent = new byte[fs.Length];
                    fs.Read(archiveContent, 0, archiveContent.Length);
                }
            }
            catch(Exception e){

            }
            TestClass result = new TestClass()
            {
                Content = archiveContent
            };
            return result;
        }
        public DateTime InvokeMethod_UserInfo(UserInfo ui)
        {
            return DateTime.Now;
        }
        #endregion
    }
}
