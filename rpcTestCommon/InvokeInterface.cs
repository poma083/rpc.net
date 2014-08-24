using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace rpcTestCommon
{
    public interface InvokeInterface
    {
        int TestSumm(int a1, int a2);
        void TestVoid();
        DateTime? TestNulable(DateTime arg);
        DateTime TestException();

        TestClass TestBufferResize(TestClass tc);
        DateTime InvokeMethod_UserInfo(UserInfo ui);
    }
}
