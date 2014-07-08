using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace rpcTestCommon
{
    public interface InvokeInterface
    {
        DateTime InvokeMethod_UserInfo(UserInfo ui);
        DateTime? InvokeMethod_ReturnNull(UserInfo ui);        
        void InvokeMethod_EmptyResult(UserInfo ui);
    }
}
