using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace rpcTestCommon
{
    [Serializable]
    public class UserInfo
    {
        public string Address { get; set; }
        public string Name { get; set; }
        public string Family { get; set; }
        public string Father { get; set; }
        public string Phone { get; set; }
        public DateTime LastDateUpdate { get; set; }
    }
}
