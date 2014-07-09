using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDUDatas
{
    [Serializable]
    public class PDURequestException : Exception
    {
        public PDURequestException()
            : base()
        {

        }
        //public PDURequestException(string message)
        //    : base(message)
        //{

        //}
        public PDURequestException(string message, Exception inner)
            : base(message, inner)
        {

        }
    }
}
