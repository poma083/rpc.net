using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PDUDatas
{
    public class PDU : IHeadPDU
    {
        #region private fields
        private HeadPDU head;
        private byte[] body;
        #endregion

        #region constructors
        public PDU(uint command_id, uint command_state, uint sequence, byte[] body)
        {
            int len = 16;
            if (body != null)
            {
                len += body.Length;
            }
            //Tools.
            head = new HeadPDU(len, command_id, command_state, sequence);
            if (body != null)
            {
                if (this.body == null)
                {
                    this.body = new byte[body.Length];
                }
                else
                {
                    Array.Resize<byte>(ref this.body, body.Length);
                }
                Array.Copy(body, 0, this.body, 0, body.Length);
            }
            //else {
            //    this.body = null;
            //}
        }
        public PDU(uint command_id, uint command_state, uint sequence)
            : this(command_id, command_state, sequence, null) { }
        public PDU()
            : this(0, 0, 0, null) { }

        public PDU(byte[] data)
        {
            if (data != null)
            {
                head = new HeadPDU();
                //Array.Copy(data, 0, head.data, 0, 16);
                head.data = data;
                if ((data.Length - 16) > 0)
                {
                    if (this.body == null)
                    {

                        this.body = new byte[data.Length - 16];
                    }
                    else
                    {
                        Array.Resize<byte>(ref this.body, data.Length - 16);
                    }
                    Array.Copy(data, 16, this.body, 0, data.Length - 16);
                }
            }
        }
        #endregion

        public void SetHeader(byte[] h)
        {
            this.head.data = h;
        }
        public void SetBody(byte[] body)
        {
            if (this.body == null)
            {
                this.body = new byte[body.Length];
            }
            else
            {
                Array.Resize<byte>(ref this.body, body.Length);
            }
            Array.Copy(body, 0, this.body, 0, body.Length);
            this.head.length = 16 + this.Body.Length;
        }
        public void AddBodyPart(byte[] BodyPart)
        {
            int prevBodyLen;
            if (this.body == null)
            {
                prevBodyLen = 0;
                this.body = new byte[BodyPart.Length];
            }
            else
            {
                prevBodyLen = this.body.Length;
                Array.Resize<byte>(ref this.body, prevBodyLen + BodyPart.Length);
            }
            Array.Copy(BodyPart, 0, this.body, prevBodyLen, BodyPart.Length);
            this.head.length += BodyPart.Length;
        }
        protected byte[] Body
        {
            get
            {
                byte[] result = null;
                if ((body != null) && (head.length > 16))
                {
                    if (body.Length !=( head.length - 16))
                    {
                        throw new IndexOutOfRangeException("body.Length != head.length");
                    }
                    result = new byte[body.Length];
                    Array.Copy(this.body, 0, result, 0, body.Length);
                }
                return result;
            }
        }
        public byte[] AllData
        {
            get
            {
                byte[] ar = new byte[Lenght];
                Array.Copy(head.data, 0, ar, 0, 16);
                if ((body != null) && (head.length > 16))
                {
                    if (body.Length != head.length-16)
                    {
                        throw new IndexOutOfRangeException("body.Length != head.length");
                    }
                    Array.Copy(body, 0, ar, 16, Lenght - 16);
                }
                return ar;
            }
        }

        #region headerAccessors
        public int Lenght
        {
            get
            {
                return head.length;
            }
        }
        public uint CommandID
        {
            get
            {
                return head.commandid;
            }
            set
            {
                head.commandid = value;
            }
        }
        public uint CommandState
        {
            get
            {
                return head.commandstate;
            }
            set
            {
                head.commandstate = value;
            }
        }
        public uint Sequence
        {
            get
            {
                return head.sequence;
            }
            set
            {
                head.sequence = value;
            }
        }

        public byte[] bLenght
        {
            get
            {
                return head.bLength;
            }
        }
        public byte[] bCommandID
        {
            get
            {
                return head.bCommandId;
            }
            set
            {
                head.bcommandid0 = value[0];
                head.bcommandid1 = value[0];
                head.bcommandid2 = value[0];
                head.bcommandid3 = value[0];
            }
        }
        public byte[] bCommandState
        {
            get
            {
                return head.bCommandState;
            }
            set
            {
                head.bcommandstate0 = value[0];
                head.bcommandstate1 = value[0];
                head.bcommandstate2 = value[0];
                head.bcommandstate3 = value[0];
            }
        }
        public byte[] bSequence
        {
            get
            {
                return head.bSequence;
            }
            set
            {
                head.bsequence0 = value[0];
                head.bsequence1 = value[0];
                head.bsequence2 = value[0];
                head.bsequence3 = value[0];
            }
        }
        public uint NextSequence()
        {
            if (Sequence == uint.MaxValue)
            {
                head.sequence = 1;
            }
            else
            {
                head.sequence++;
            }
            return Sequence;
        }
        #endregion

        public static void IncreaseLength<T>(ref T[] arr, int newlen)
        {
            T[] tmp = new T[newlen];
            int len = Math.Min(arr.Length, newlen);
            Array.Copy(arr, 0, tmp, 0, len);
            arr = tmp;
        }
    }
}
