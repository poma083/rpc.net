using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDUDatas
{
    public interface IHeadPDU
    {
        int Lenght { get; }
        uint CommandID { get; set; }
        uint CommandState { get; set; }
        uint Sequence { get; set; }
        byte[] bLenght { get; }
        byte[] bCommandID { get; set; }
        byte[] bCommandState { get; set; }
        byte[] bSequence { get; set; }
        uint NextSequence();
    }
}
