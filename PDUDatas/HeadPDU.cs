using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;

namespace PDUDatas
{
    [StructLayoutAttribute(LayoutKind.Explicit)]
    public class HeadPDU
    {
        public HeadPDU()
        {
        }

        public HeadPDU(byte[] _data)
        {
            data = _data;
        }

        public HeadPDU(int length, MessageType commandid, uint commandstate, uint sequence)
        {
            this.length = length;
            this.commandid = commandid;
            this.commandstate = commandstate;
            this.sequence = sequence;
        }

        public byte[] bLength
        {
            get
            {
                byte[] res = new byte[4];
                res[0] = blength0;
                res[1] = blength1;
                res[2] = blength2;
                res[3] = blength3;
                return res;
            }
        }
        public byte[] bCommandId
        {
            get
            {
                byte[] res = new byte[4];
                res[0] = bcommandid0;
                res[1] = bcommandid1;
                res[2] = bcommandid2;
                res[3] = bcommandid3;
                return res;
            }
        }
        public byte[] bCommandState
        {
            get
            {
                byte[] res = new byte[4];
                res[0] = bcommandstate0;
                res[1] = bcommandstate1;
                res[2] = bcommandstate2;
                res[3] = bcommandstate3;
                return res;
            }
        }
        public byte[] bSequence
        {
            get
            {
                byte[] res = new byte[4];
                res[0] = bsequence0;
                res[1] = bsequence1;
                res[2] = bsequence2;
                res[3] = bsequence3;
                return res;
            }
        }
        public byte[] data
        {
            get
            {
                byte[] res = new byte[16];
                res[0] = data0;
                res[1] = data1;
                res[2] = data2;
                res[3] = data3;
                res[4] = data4;
                res[5] = data5;
                res[6] = data6;
                res[7] = data7;
                res[8] = data8;
                res[9] = data9;
                res[10] = data10;
                res[11] = data11;
                res[12] = data12;
                res[13] = data13;
                res[14] = data14;
                res[15] = data15;
                return res;
            }
            set
            {
                data0 = value[0];
                data1 = value[1];
                data2 = value[2];
                data3 = value[3];
                data4 = value[4];
                data5 = value[5];
                data6 = value[6];
                data7 = value[7];
                data8 = value[8];
                data9 = value[9];
                data10 = value[10];
                data11 = value[11];
                data12 = value[12];
                data13 = value[13];
                data14 = value[14];
                data15 = value[15];
            }
        }

        [FieldOffsetAttribute(0)]
        public byte data3;
        [FieldOffsetAttribute(1)]
        public byte data2;
        [FieldOffsetAttribute(2)]
        public byte data1;
        [FieldOffsetAttribute(3)]
        public byte data0;
        [FieldOffsetAttribute(4)]
        public byte data7;
        [FieldOffsetAttribute(5)]
        public byte data6;
        [FieldOffsetAttribute(6)]
        public byte data5;
        [FieldOffsetAttribute(7)]
        public byte data4;
        [FieldOffsetAttribute(8)]
        public byte data11;
        [FieldOffsetAttribute(9)]
        public byte data10;
        [FieldOffsetAttribute(10)]
        public byte data9;
        [FieldOffsetAttribute(11)]
        public byte data8;
        [FieldOffsetAttribute(12)]
        public byte data15;
        [FieldOffsetAttribute(13)]
        public byte data14;
        [FieldOffsetAttribute(14)]
        public byte data13;
        [FieldOffsetAttribute(15)]
        public byte data12;

        [FieldOffsetAttribute(0)]
        public int length;
        [FieldOffsetAttribute(0)]
        public byte blength3;
        [FieldOffsetAttribute(1)]
        public byte blength2;
        [FieldOffsetAttribute(2)]
        public byte blength1;
        [FieldOffsetAttribute(3)]
        public byte blength0;

        [FieldOffsetAttribute(4)]
        public MessageType commandid;
        [FieldOffsetAttribute(4)]
        public byte bcommandid3;
        [FieldOffsetAttribute(5)]
        public byte bcommandid2;
        [FieldOffsetAttribute(6)]
        public byte bcommandid1;
        [FieldOffsetAttribute(7)]
        public byte bcommandid0;

        [FieldOffsetAttribute(8)]
        public uint commandstate;
        [FieldOffsetAttribute(8)]
        public byte bcommandstate3;
        [FieldOffsetAttribute(9)]
        public byte bcommandstate2;
        [FieldOffsetAttribute(10)]
        public byte bcommandstate1;
        [FieldOffsetAttribute(11)]
        public byte bcommandstate0;

        [FieldOffsetAttribute(12)]
        public uint sequence;
        [FieldOffsetAttribute(12)]
        public byte bsequence3;
        [FieldOffsetAttribute(13)]
        public byte bsequence2;
        [FieldOffsetAttribute(14)]
        public byte bsequence1;
        [FieldOffsetAttribute(15)]
        public byte bsequence0;
    }
}
