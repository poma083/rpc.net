using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace PDUDatas
{
    //---------------------------------------------------------------------------//
    //-------------------------------BindTransceiver-----------------------------//
    //---------------------------------------------------------------------------//
    public sealed class PDUBindTransceiver : PDU
    {
        private int pos = 0;

        public PDUBindTransceiver(byte[] data)
            : base(data)
        {
            if (CommandID != 0x00000009)
            {
                throw new ArgumentException("Переданные данные не являются командой BindTransceiver");
            }
        }
        public PDUBindTransceiver(uint _commandState, uint _prevsequence, string systemId, string pass, uint timeout)
            : base(0x00000009, _commandState, _prevsequence)
        {
            AddBodyPart(Encoding.ASCII.GetBytes(systemId));
            AddBodyPart(new byte[] { 0x00 });
            string sss = SystemID;
            AddBodyPart(Encoding.ASCII.GetBytes(pass));
            AddBodyPart(new byte[] { 0x00 });
            byte[] l = new byte[4];
            Tools.ConvertUIntToArray(timeout, out l);
            AddBodyPart(l);
        }

        public void GetData(out string systemId, out string password, out uint timeout)
        {
            pos = 0;
            StringBuilder sb = new StringBuilder();
            while (Body[pos] != 0x00)
            {
                sb.Append(Convert.ToChar(Body[pos]));
                ++pos;
            }
            systemId = sb.ToString();
            ++pos;
            sb.Clear();
            while (Body[pos] != 0x00)
            {
                sb.Append(Convert.ToChar(Body[pos]));
                ++pos;
            }
            password = sb.ToString();
            ++pos;

            uint i = 0;
            Tools.ConvertArrayToUInt(this.Body, pos, ref i);
            timeout = i;
        }
        public string SystemID
        {
            get
            {
                pos = 0;
                byte[] localBody = Body;

                StringBuilder sb = new StringBuilder();
                while (localBody[pos] != 0x00)
                {
                    sb.Append(Convert.ToChar(localBody[pos]));
                    ++pos;
                }
                return sb.ToString();
            }
        }
        public string Password
        {
            get
            {
                pos = 0;

                while (Body[pos] != 0x00)
                {
                    ++pos;
                }
                ++pos;
                StringBuilder sb = new StringBuilder();
                while (Body[pos] != 0x00)
                {
                    sb.Append(Convert.ToChar(Body[pos]));
                    ++pos;
                }
                return sb.ToString();
            }
        }
        public uint Timeout
        {
            get
            {
                pos = 0;
                while (Body[pos] != 0x00)
                {
                    ++pos;
                }
                ++pos;
                while (Body[pos] != 0x00)
                {
                    ++pos;
                }
                ++pos;

                uint i = 0;
                Tools.ConvertArrayToUInt(this.Body, pos, ref i);
                return i;
            }
        }
    }
    public sealed class PDUBindTransceiverResp : PDU
    {
        public PDUBindTransceiverResp(uint _commandState, uint _prevsequence, string _sid)
            : base(0x80000009, _commandState, _prevsequence)
        {
            this.AddBodyPart(Encoding.ASCII.GetBytes(_sid));
            this.AddBodyPart(new byte[] { 0x00 });
        }
        public PDUBindTransceiverResp(byte[] data)
            : base(data)
        {
            if (CommandID != 0x80000009)
            {
                throw new ArgumentException("Переданные данные не являются командой BindTransceiverResp");
            }
        }

        public string SystemID
        {
            get
            {
                StringBuilder sb = new StringBuilder();
                int i = 0;
                while (Body[i] != 0x00)
                {
                    sb.Append(Convert.ToChar(Body[i]));
                    i++;
                }
                return sb.ToString();
            }
        }
    }
    //---------------------------------------------------------------------------//
    //--------------------------------GenericNack--------------------------------//
    //---------------------------------------------------------------------------//
    public sealed class PDUGenericNack : PDU
    {
        public PDUGenericNack(byte[] data)
            : base(data)
        {
            if (CommandID != 0x80000000)
            {
                throw new ArgumentException("Переданные данные не являются командой GenericNack");
            }
        }
        public PDUGenericNack(uint _commandState, uint _prevsequence)
            : base(0x80000000, _commandState, _prevsequence)
        {
        }
    }
    //---------------------------------------------------------------------------//
    //--------------------------------EnquireLink--------------------------------//
    //---------------------------------------------------------------------------//
    public sealed class PDUEnquireLink : PDU
    {
        public PDUEnquireLink(byte[] data)
            : base(data)
        {
            if (CommandID != 0x00000015)
            {
                throw new ArgumentException("Переданные данные не являются командой EnquireLink");
            }
        }
    }
    public sealed class PDUEnquireLinkResp : PDU
    {
        public PDUEnquireLinkResp(uint _commandState, uint _prevsequence)
            : base(0x80000015, _commandState, _prevsequence)
        {
            if (CommandID != 0x80000015)
            {
                throw new ArgumentException("Переданные данные не являются командой EnquireLinkResp");
            }
        }
    }
    //---------------------------------------------------------------------------//
    //---------------------------------PDUInvoke---------------------------------//
    //---------------------------------------------------------------------------//
    public sealed class PDUInvoke : PDU
    {
        public PDUInvoke(byte[] data)
            : base(data)
        {
            if (CommandID != 0x00000003)
            {
                throw new ArgumentException("Переданные данные не являются командой Invoke");
            }
        }
        public PDUInvoke(uint commandState, uint sequence, string assName, string typeName, byte isInstance, string method, BindingFlags bindingFlags, object[] arguments)
            : base(0x00000003, commandState, sequence)
        {
            this.AddBodyPart(Encoding.ASCII.GetBytes(assName));
            this.AddBodyPart(new byte[] { 0x00 });
            this.AddBodyPart(Encoding.ASCII.GetBytes(typeName));
            this.AddBodyPart(new byte[] { 0x00 });
            this.AddBodyPart(new byte[] { isInstance });
            this.AddBodyPart(Encoding.ASCII.GetBytes(method));
            this.AddBodyPart(new byte[] { 0x00 });
            byte[] bf = new byte[4];
            Tools.ConvertIntToArray((int)bindingFlags, out bf);
            this.AddBodyPart(bf);

            MemoryStream stream = new MemoryStream();
            BinaryFormatter ff = new BinaryFormatter();
            ff.Serialize(stream, arguments);
            byte[] serializeData = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(serializeData, 0, (int)stream.Length);
            AddBodyPart(serializeData);
        }

        public void GetData(out string assName, out string typeName, out byte isInstance, out string method, out BindingFlags bindingFlags, out object[] arguments)
        {
            int i = 0;
            byte[] localBody = Body;

            StringBuilder sb = new StringBuilder();
            sb.Clear();
            while (localBody[i] != 0x00)
            {
                sb.Append(Convert.ToChar(localBody[i]));
                ++i;
            }
            assName = sb.ToString();
            ++i;
            while (localBody[i] != 0x00)
            {
                sb.Append(Convert.ToChar(localBody[i]));
                ++i;
            }
            typeName = sb.ToString();
            ++i;
            isInstance = localBody[i];
            ++i;
            sb.Clear();
            while (localBody[i] != 0x00)
            {
                sb.Append(Convert.ToChar(localBody[i]));
                ++i;
            }
            method = sb.ToString();
            ++i;
            int bindingFlagsTmp = 0;
            Tools.ConvertArrayToInt(localBody, i, ref bindingFlagsTmp);
            bindingFlags = (BindingFlags)bindingFlagsTmp;
            i += 4;

            MemoryStream stream = new MemoryStream();
            stream.Write(localBody, i, localBody.Length - i);
            stream.Position = 0;
            BinaryFormatter ff = new BinaryFormatter();
            arguments = (object[])ff.Deserialize(stream);
        }
        public Assembly Assembly
        {
            get
            {
                int i = 0;
                byte[] localBody = Body;

                StringBuilder sb = new StringBuilder();
                while (localBody[i] != 0x00)
                {
                    sb.Append(Convert.ToChar(localBody[i]));
                    ++i;
                }
                string assName = sb.ToString();
                return AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName == assName).FirstOrDefault();
            }
        }
        public Type InstanceType
        {
            get
            {
                int i = 0;
                byte[] localBody = Body;

                StringBuilder sb = new StringBuilder();
                while (localBody[i] != 0x00)
                {
                    sb.Append(Convert.ToChar(localBody[i]));
                    ++i;
                }
                string assName = sb.ToString();
                Assembly ass = AppDomain.CurrentDomain.GetAssemblies().Where(a => a.FullName == assName).FirstOrDefault();
                if (ass == null)
                {
                    throw new NullReferenceException(String.Format("Не найдена сборка по имени {0}", assName));
                }
                ++i;
                sb.Clear();
                while (localBody[i] != 0x00)
                {
                    sb.Append(Convert.ToChar(localBody[i]));
                    ++i;
                }
                string typeName = sb.ToString();
                Type[] types = ass.GetTypes();
                return types.Where(t => t.FullName == typeName).FirstOrDefault();
            }
        }
        public byte IsInstance
        {
            get
            {
                int i = 0;
                byte[] localBody = Body;

                while (localBody[i] != 0x00)
                {
                    ++i;
                }
                ++i;
                while (localBody[i] != 0x00)
                {
                    ++i;
                }
                ++i;
                return localBody[i];
            }
        }
        public string Method
        {
            get
            {
                int i = 0;
                byte[] localBody = Body;


                while (localBody[i] != 0x00)
                {
                    ++i;
                }
                ++i;
                while (localBody[i] != 0x00)
                {
                    ++i;
                }
                ++i;
                ++i;
                StringBuilder sb = new StringBuilder();
                while (localBody[i] != 0x00)
                {
                    sb.Append(Convert.ToChar(localBody[i]));
                    ++i;
                }
                return sb.ToString();
            }
        }
        public BindingFlags BindingFlags
        {
            get
            {
                int i = 0;
                byte[] localBody = Body;

                while (localBody[i] != 0x00)
                {
                    ++i;
                }
                ++i;
                while (localBody[i] != 0x00)
                {
                    ++i;
                }
                ++i;
                ++i;
                while (localBody[i] != 0x00)
                {
                    ++i;
                }
                ++i;
                uint result = 0;
                Tools.ConvertArrayToUInt(localBody, i, ref result);
                return (BindingFlags)result;
            }
        }
        public object[] Arguments
        {
            get
            {
                int i = 0;
                byte[] localBody = Body;

                while (localBody[i] != 0x00)
                {
                    ++i;
                }
                ++i;
                while (localBody[i] != 0x00)
                {
                    ++i;
                }
                ++i;
                ++i;
                while (localBody[i] != 0x00)
                {
                    ++i;
                }
                ++i;
                i += 4;

                MemoryStream stream = new MemoryStream();
                stream.Write(localBody, i, localBody.Length - i);
                stream.Position = 0;
                BinaryFormatter ff = new BinaryFormatter();
                return (object[])ff.Deserialize(stream);
            }
        }
    }
    public sealed class PDUInvokeResp : PDU
    {
        [Serializable]
        class SerializableException
        {
            public SerializableException()
            {

            }
            public SerializableException(Exception ex)
            {
                fields = new Dictionary<string, object>();
                this.Type = ex.GetType();
                FieldInfo[] fi_l = this.Type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);
                foreach (FieldInfo fi in fi_l)
                {
                    if (fi.FieldType.IsPrimitive)
                    {

                    }
                    else
                    {
                        SerializableAttribute sa = Attribute.GetCustomAttribute(fi.FieldType, typeof(SerializableAttribute)) as SerializableAttribute;
                        if (sa == null)
                        {
                            continue;
                        }
                    }
                    if (!fields.ContainsKey(fi.Name))
                    {
                        fields.Add(fi.Name, null);
                    }
                    fields[fi.Name] = fi.GetValue(ex);
                }
            }
            public Type Type { get; set; }
            public Dictionary<string, object> fields { get; set; }

            public Exception GetException()
            {
                object instance = null;
                ConstructorInfo ci = this.Type.GetConstructor(new Type[] { });
                if (ci != null)
                {
                    instance = ci.Invoke(new object[] { });
                }
                if (instance != null)
                {
                    FieldInfo[] fi_l = this.Type.GetFields(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly | BindingFlags.Instance);
                    foreach (FieldInfo fi in fi_l)
                    {
                        if (fi.FieldType.IsPrimitive)
                        {

                        }
                        else
                        {
                            SerializableAttribute sa = Attribute.GetCustomAttribute(fi.FieldType, typeof(SerializableAttribute)) as SerializableAttribute;
                            if (sa == null)
                            {
                                continue;
                            }
                        }
                        if (fields.ContainsKey(fi.Name))
                        {
                            fi.SetValue(instance, fields[fi.Name]);
                        }
                    }
                    return (Exception)instance;
                }
                return null;
            }
        }

        [Serializable]
        class ResultData
        {
            public object data;
            //public SerializableException exception;
            public Exception exception;
        }
        public PDUInvokeResp(byte[] data)
            : base(data)
        {
            if (CommandID != 0x80000003)
            {
                throw new ArgumentException("Переданные данные не являются командой InvokeResp");
            }
        }
        public PDUInvokeResp(uint _commandState, uint _sequence, string typeName, object data, Exception ex)
            : base(0x80000003, _commandState, _sequence)
        {
            this.AddBodyPart(Encoding.ASCII.GetBytes(typeName));
            this.AddBodyPart(new byte[] { 0x00 });

            ResultData resultData = new ResultData()
            {
                data = data,
                exception = ex// != null ? new SerializableException(ex) : null
            };
            MemoryStream stream = new MemoryStream();
            BinaryFormatter ff = new BinaryFormatter();
            ff.Serialize(stream, resultData);
            byte[] serializeData = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(serializeData, 0, (int)stream.Length);
            AddBodyPart(serializeData);
        }

        public void GetData(out string typeName, out object data)
        {
            int i = 0;
            byte[] localBody = Body;

            StringBuilder sb = new StringBuilder();
            while (localBody[i] != 0x00)
            {
                sb.Append(Convert.ToChar(localBody[i]));
                ++i;
            }
            typeName = sb.ToString();
            ++i;

            MemoryStream stream = new MemoryStream();
            stream.Write(localBody, i, localBody.Length - i);
            stream.Position = 0;
            BinaryFormatter ff = new BinaryFormatter();
            ResultData resultData = (ResultData)ff.Deserialize(stream);
            data = resultData.data;
        }
        public string TypeName
        {
            get
            {
                int i = 0;
                byte[] localBody = Body;

                StringBuilder sb = new StringBuilder();
                while (localBody[i] != 0x00)
                {
                    sb.Append(Convert.ToChar(localBody[i]));
                    ++i;
                }
                return sb.ToString();
            }
        }
        public TEntity GetInvokeResult<TEntity>()
        {
            int i = 0;
            byte[] localBody = Body;


            while (localBody[i] != 0x00)
            {
                ++i;
            }
            ++i;

            MemoryStream stream = new MemoryStream();
            stream.Write(localBody, i, localBody.Length - i);
            stream.Position = 0;
            BinaryFormatter ff = new BinaryFormatter();
            ResultData resultData = (ResultData)ff.Deserialize(stream);
            if (resultData.exception != null)
            {
                throw new PDURequestException("Обнаружена ошибка при получении данных", resultData.exception);//.GetException());
            }
            return (TEntity)resultData.data;
        }
        //public object GetInvokeResult()
        //{
        //    int i = 0;
        //    byte[] localBody = Body;


        //    while (localBody[i] != 0x00)
        //    {
        //        ++i;
        //    }
        //    ++i;

        //    MemoryStream stream = new MemoryStream();
        //    stream.Write(localBody, i, localBody.Length - i);
        //    stream.Position = 0;
        //    BinaryFormatter ff = new BinaryFormatter();
        //    ResultData resultData = (ResultData)ff.Deserialize(stream);
        //    return resultData.data;
        //}
    }
    //---------------------------------------------------------------------------//
    //---------------------------------------------------------------------------//
    //---------------------------------------------------------------------------//

}
