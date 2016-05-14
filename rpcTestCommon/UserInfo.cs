using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace rpcTestCommon
{
    [Serializable]
    public class Advanced
    {
        public DateTime dt0 { get; set; }
        public DateTime dt1 { get; set; }
        public DateTime dt2 { get; set; }
        public DateTime dt3 { get; set; }
        public DateTime dt4 { get; set; }
        public DateTime dt5 { get; set; }
        public DateTime dt6 { get; set; }
        public DateTime dt7 { get; set; }

        public Advanced()
        {
            dt0 = DateTime.Now;
            dt1 = DateTime.Now;
            dt2 = DateTime.Now;
            dt3 = DateTime.Now;
            dt4 = DateTime.Now;
            dt5 = DateTime.Now;
            dt6 = DateTime.Now;
            dt7 = DateTime.Now;
        }
    }
    [Serializable]
    public class UserInfo
    {
        public string Address { get; set; }
        public string Name { get; set; }
        public string Family { get; set; }
        public string Father { get; set; }
        public string Phone { get; set; }
        public DateTime DateCreate { get; set; }
        public DateTime LastDateUpdate { get; set; }
        public DateTime[] datas;

        public UserInfo()
        {
            //DateCreate = DateTime.Now;
            datas = new DateTime[60];
            for (int i = 0; i < 60; i++ )
            {
                datas[i] = DateTime.Now;
            }
        }
    }
}
