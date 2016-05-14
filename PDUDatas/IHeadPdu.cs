using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDUDatas
{
    public enum MessageType : uint
    {
        RespMask =            0x80000000,
        EnquireLinkMask =     0x00000001,
        BindTransceiverMask = 0x00000002,
        ByNameMask =          0x00000200,
        InvokeMask =          0x00000100,
        SecureMask =          0x01000000,
        WaitMask =            0x00001000,

        GenericNack = RespMask,
        EnquireLink = EnquireLinkMask,
        EnquireLinkResp = EnquireLink | RespMask,
        BindTransceiver = BindTransceiverMask,
        BindTransceiverResp = BindTransceiver | RespMask,
        Invoke = InvokeMask,
        InvokeByName = InvokeMask | ByNameMask,
        InvokeSecureByName = InvokeMask | ByNameMask | SecureMask,
        InvokeResp = InvokeMask | RespMask,
        InvokeSecureByNameResp = InvokeMask | ByNameMask | SecureMask | RespMask,
        Wait = WaitMask,
        WaitByName = WaitMask | ByNameMask,
        WaitResp = WaitMask | RespMask
    }
    public enum WaitType { WaitConcurently, WaitAll };
    
    public interface IHeadPDU
    {
        int Lenght { get; }
        MessageType CommandID { get; set; }
        uint CommandState { get; set; }
        uint Sequence { get; set; }
        byte[] bLenght { get; }
        byte[] bCommandID { get; set; }
        byte[] bCommandState { get; set; }
        byte[] bSequence { get; set; }
        uint NextSequence();
    }
}
