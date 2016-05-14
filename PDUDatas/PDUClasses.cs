using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography.X509Certificates;

namespace PDUDatas
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
    //---------------------------------------------------------------------------//
    //-------------------------------BindTransceiver-----------------------------//
    //---------------------------------------------------------------------------//
    public sealed class PDUBindTransceiver : PDU
    {
        private int pos = 0;

        public PDUBindTransceiver(byte[] data)
            : base(data)
        {
            if (CommandID != MessageType.BindTransceiver)
            {
                throw new ArgumentException("Переданные данные не являются командой BindTransceiver");
            }
        }
        public PDUBindTransceiver(uint _commandState, uint _prevsequence, string systemId, string pass, uint timeout, string configurationName)
            : base(MessageType.BindTransceiver, _commandState, _prevsequence)
        {
            AddBodyPart(Encoding.ASCII.GetBytes(systemId));
            AddBodyPart(new byte[] { 0x00 });
            string sss = SystemID;
            AddBodyPart(Encoding.ASCII.GetBytes(pass));
            AddBodyPart(new byte[] { 0x00 });
            byte[] l = new byte[4];
            Tools.ConvertUIntToArray(timeout, out l);
            AddBodyPart(l);
            AddBodyPart(Encoding.ASCII.GetBytes(configurationName));
            AddBodyPart(new byte[] { 0x00 });
        }

        public void GetData(out string systemId, out string password, out uint timeout, out string configurationName)
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
            pos += 4;
            sb.Clear();
            while (Body[pos] != 0x00)
            {
                sb.Append(Convert.ToChar(Body[pos]));
                ++pos;
            }
            configurationName = sb.ToString();
            ++pos;
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
        public string ConfigurationName
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
                pos += 4;
                StringBuilder sb = new StringBuilder();
                while (Body[pos] != 0x00)
                {
                    sb.Append(Convert.ToChar(Body[pos]));
                    ++pos;
                }
                return sb.ToString();
            }
        }
    }
    public sealed class PDUBindTransceiverResp : PDU
    {
        public PDUBindTransceiverResp(uint _commandState, uint _prevsequence, string _sid)
            : base(MessageType.BindTransceiverResp, _commandState, _prevsequence)
        {
            this.AddBodyPart(Encoding.ASCII.GetBytes(_sid));
            this.AddBodyPart(new byte[] { 0x00 });
        }
        public PDUBindTransceiverResp(byte[] data)
            : base(data)
        {
            if (CommandID != MessageType.BindTransceiverResp)
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
            if (CommandID != MessageType.GenericNack)
            {
                throw new ArgumentException("Переданные данные не являются командой GenericNack");
            }
        }
        public PDUGenericNack(uint _commandState, uint _prevsequence)
            : base(MessageType.GenericNack, _commandState, _prevsequence)
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
            if (CommandID != MessageType.EnquireLink)
            {
                throw new ArgumentException("Переданные данные не являются командой EnquireLink");
            }
        }
    }
    public sealed class PDUEnquireLinkResp : PDU
    {
        public PDUEnquireLinkResp(uint _commandState, uint _prevsequence)
            : base(MessageType.EnquireLinkResp, _commandState, _prevsequence)
        {
            if (CommandID != MessageType.EnquireLinkResp)
            {
                throw new ArgumentException("Переданные данные не являются командой EnquireLinkResp");
            }
        }
    }
    //---------------------------------------------------------------------------//
    //---------------------------------PDUInvoke---------------------------------//
    //---------------------------------------------------------------------------//
    public abstract class PDUResp : PDU
    {
        public PDUResp(byte[] data)
            : base(data)
        {
        }
        public PDUResp(MessageType _commandId, uint _commandState, uint _sequence, string typeName)
            : base(_commandId, _commandState, _sequence)
        {
            this.AddBodyPart(Encoding.ASCII.GetBytes(typeName));
            this.AddBodyPart(new byte[] { 0x00 });
        }

        public abstract void GetData(out string typeName, out object data);
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
        public abstract TEntity GetInvokeResult<TEntity>();
    }
    
    public sealed class PDUInvoke : PDU
    {
        public PDUInvoke(byte[] data)
            : base(data)
        {
            if (CommandID != MessageType.Invoke)
            {
                throw new ArgumentException("Переданные данные не являются командой Invoke");
            }
        }
        public PDUInvoke(uint commandState, uint sequence, string assName, string typeName, byte isInstance, string method, BindingFlags bindingFlags, object[] arguments)
            : base(MessageType.Invoke, commandState, sequence)
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
    public sealed class PDUInvokeByName : PDU
    {
        public PDUInvokeByName(byte[] data)
            : base(data)
        {
            if (CommandID != MessageType.InvokeByName)
            {
                throw new ArgumentException("Переданные данные не являются командой PDUInvokeByName");
            }
        }
        public PDUInvokeByName(uint commandState, uint sequence, string invokeName, object[] arguments)
            : base(MessageType.InvokeByName, commandState, sequence)
        {
            this.AddBodyPart(Encoding.ASCII.GetBytes(invokeName));
            this.AddBodyPart(new byte[] { 0x00 });

            MemoryStream stream = new MemoryStream();
            BinaryFormatter ff = new BinaryFormatter();
            ff.Serialize(stream, arguments);
            byte[] serializeData = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(serializeData, 0, (int)stream.Length);
            AddBodyPart(serializeData);
        }

        public void GetData(out string invokeName, out object[] arguments)
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
            invokeName = sb.ToString();
            ++i;
            MemoryStream stream = new MemoryStream();
            stream.Write(localBody, i, localBody.Length - i);
            stream.Position = 0;
            BinaryFormatter ff = new BinaryFormatter();
            arguments = (object[])ff.Deserialize(stream);
        }
        public string InvokeName
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

                MemoryStream stream = new MemoryStream();
                stream.Write(localBody, i, localBody.Length - i);
                stream.Position = 0;
                BinaryFormatter ff = new BinaryFormatter();
                return (object[])ff.Deserialize(stream);
            }
        }
    }
    public sealed class PDUInvokeResp : PDUResp
    {
        public PDUInvokeResp(byte[] data)
            : base(data)
        {
            if (CommandID != MessageType.InvokeResp)
            {
                throw new ArgumentException("Переданные данные не являются командой InvokeResp");
            }
        }
        public PDUInvokeResp(uint _commandState, uint _sequence, string typeName, object data, Exception ex)
            : base(MessageType.InvokeResp, _commandState, _sequence, typeName)
        {
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

        public override void GetData(out string typeName, out object data)
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
        public override TEntity GetInvokeResult<TEntity>()
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
    }
    //---------------------------------------------------------------------------//
    //------------------------------PDUInvokeSecure------------------------------//
    //---------------------------------------------------------------------------//
    public sealed class PDUInvokeSecureByName : PDU
    {
        /// <summary>
        /// вызывается на серверной стороне
        /// </summary>
        public PDUInvokeSecureByName(byte[] data)
            : base(data)
        {
            if (CommandID != MessageType.InvokeSecureByName)
            {
                throw new ArgumentException("Переданные данные не являются командой PDUInvokeSecureByName");
            }
        }
        /// <summary>
        /// вызывается на стороне клииеинта
        /// </summary>
        public PDUInvokeSecureByName(uint commandState, uint sequence, string invokeName, object[] arguments,
            StoreName clientCertStore, StoreLocation clientCertStoreLocation, string clientCertThumbprint, X509Certificate2 serverPublicCertificate)
            : base(MessageType.InvokeSecureByName, commandState, sequence)
        {
            this.AddBodyPart(Encoding.ASCII.GetBytes(invokeName));
            this.AddBodyPart(new byte[] { 0x00 });

            X509Certificate2 clientCertificate = SCZI.FindCertificate(clientCertStore, clientCertStoreLocation, clientCertThumbprint);
            byte[] clientCertificateArray = clientCertificate.RawData;

            byte[] certDataLengthBuffer = new byte[4];
            Tools.ConvertIntToArray(clientCertificateArray.Length, out certDataLengthBuffer);
            AddBodyPart(certDataLengthBuffer);
            AddBodyPart(clientCertificateArray);

            MemoryStream stream = new MemoryStream();
            BinaryFormatter ff = new BinaryFormatter();
            ff.Serialize(stream, arguments);
            byte[] serializeData = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(serializeData, 0, (int)stream.Length);
            byte[] encryptData = SCZI.Encrypt(serializeData, serverPublicCertificate);
            AddBodyPart(encryptData);
        }
        /// <summary>
        /// вызывается на серверной стороне в основном
        /// </summary>
        public void GetData(out string invokeName, out object[] arguments, out byte[] clientPublicCertificate,
            StoreName serverCertStore, StoreLocation serverCertStoreLocation, string serverCertThumbprint)
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
            invokeName = sb.ToString();
            ++i;

            int certificateLength = 0;
            Tools.ConvertArrayToInt(localBody, i, ref certificateLength);
            i += 4;
            clientPublicCertificate = new byte[certificateLength];
            Array.Copy(localBody, i, clientPublicCertificate, 0, certificateLength);
            i += certificateLength;

            X509Certificate2 serverCertificate = SCZI.FindCertificate(serverCertStore, serverCertStoreLocation, serverCertThumbprint);

            byte[] data2Decrypt = new byte[localBody.Length - i];
            Array.Copy(localBody, i, data2Decrypt, 0, data2Decrypt.Length);
            byte[] data = SCZI.Decrypt(data2Decrypt, serverCertificate);
            MemoryStream stream = new MemoryStream();
            stream.Write(data, 0, data.Length);
            stream.Position = 0;
            BinaryFormatter ff = new BinaryFormatter();
            arguments = (object[])ff.Deserialize(stream);
        }
        public string InvokeName
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
    }
    public sealed class PDUInvokeSecureResp : PDUResp
    {
        StoreName clientCertStore;
        StoreLocation clientCertStoreLocation;
        string clientCertThumbprint;

        public PDUInvokeSecureResp(byte[] data,
                                    StoreName clientCertStore,
                                    StoreLocation clientCertStoreLocation,
                                    string clientCertThumbprint)
            : base(data)
        {
            if (CommandID != MessageType.InvokeSecureByNameResp)
            {
                throw new ArgumentException("Переданные данные не являются командой InvokeSecureResp");
            }
            this.clientCertStore = clientCertStore;
            this.clientCertStoreLocation = clientCertStoreLocation;
            this.clientCertThumbprint = clientCertThumbprint;
        }
        public PDUInvokeSecureResp(uint _commandState, uint _sequence, string typeName, object data, Exception ex,
            X509Certificate2 clientPublicCertificate)
            : base(MessageType.InvokeSecureByNameResp, _commandState, _sequence, typeName)
        {
            ResultData resultData = new ResultData()
            {
                data = data,
                exception = ex
            };
            MemoryStream stream = new MemoryStream();
            BinaryFormatter ff = new BinaryFormatter();
            ff.Serialize(stream, resultData);
            byte[] serializeData = new byte[stream.Length];
            stream.Position = 0;
            stream.Read(serializeData, 0, (int)stream.Length);
            byte[] encryptData = SCZI.Encrypt(serializeData, clientPublicCertificate);
            AddBodyPart(encryptData);
        }

        public override void GetData(out string typeName, out object data)
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

            X509Certificate2 clientCertificate = SCZI.FindCertificate(clientCertStore, clientCertStoreLocation, clientCertThumbprint);

            byte[] data2Decrypt = new byte[localBody.Length - i];
            Array.Copy(localBody, i, data2Decrypt, 0, data2Decrypt.Length);
            byte[] decryptData = SCZI.Decrypt(data2Decrypt, clientCertificate);
            MemoryStream stream = new MemoryStream();
            stream.Write(decryptData, 0, decryptData.Length);
            stream.Position = 0;
            BinaryFormatter ff = new BinaryFormatter();
            ResultData resultData = (ResultData)ff.Deserialize(stream);
            data = resultData.data;
        }
        public override TEntity GetInvokeResult<TEntity>()
        {
            int i = 0;
            byte[] localBody = Body;


            while (localBody[i] != 0x00)
            {
                ++i;
            }
            ++i;

            X509Certificate2 clientCertificate = SCZI.FindCertificate(clientCertStore, clientCertStoreLocation, clientCertThumbprint);

            byte[] data2Decrypt = new byte[localBody.Length - i];
            Array.Copy(localBody, i, data2Decrypt, 0, data2Decrypt.Length);
            byte[] decryptData = SCZI.Decrypt(data2Decrypt, clientCertificate);
            MemoryStream stream = new MemoryStream();
            stream.Write(decryptData, 0, decryptData.Length);
            stream.Position = 0;
            BinaryFormatter ff = new BinaryFormatter();
            ResultData resultData = (ResultData)ff.Deserialize(stream);
            if (resultData.exception != null)
            {
                throw new PDURequestException("Обнаружена ошибка при получении данных", resultData.exception);
            }
            return (TEntity)resultData.data;
        }
    }
    //---------------------------------------------------------------------------//
    //----------------------------------PDUWait----------------------------------//
    //---------------------------------------------------------------------------//
    public sealed class PDUWait : PDU
    {
        /// <summary>
        /// вызывается на серверной стороне
        /// </summary>
        public PDUWait(byte[] data)
            : base(data)
        {
            if (CommandID != MessageType.Wait)
            {
                throw new ArgumentException("Переданные данные не являются командой PDUWait");
            }
        }
        /// <summary>
        /// вызывается на стороне клииеинта
        /// </summary>
        public PDUWait(uint commandState, uint sequence, string key, WaitType type)
            : base(MessageType.Wait, commandState, sequence)
        {
            this.AddBodyPart(Encoding.ASCII.GetBytes(key));
            this.AddBodyPart(new byte[] { 0x00 });
            byte[] bt = new byte[4];
            Tools.ConvertIntToArray((int)type, out bt);
            this.AddBodyPart(bt);
        }
        public void GetData(out string key, out WaitType type)
        {
            int i = 0;
            byte[] localBody = Body;

            StringBuilder sb = new StringBuilder();
            while (localBody[i] != 0x00)
            {
                sb.Append(Convert.ToChar(localBody[i]));
                ++i;
            }
            key = sb.ToString();
            ++i;
            
            int typeTmp = 0;
            Tools.ConvertArrayToInt(localBody, i, ref typeTmp);
            type = (WaitType)typeTmp;
            i += 4;
        }
    }
    public sealed class PDUWaitResp : PDU
    {
        public PDUWaitResp(byte[] data)
            : base(data)
        {
            if (CommandID != MessageType.WaitResp)
            {
                throw new ArgumentException("Переданные данные не являются командой WaitResp");
            }
        }
        public PDUWaitResp(uint _commandState, uint _sequence)
            : base(MessageType.WaitResp, _commandState, _sequence)
        {
        }
    }
    //---------------------------------------------------------------------------//
    //---------------------------------------------------------------------------//
    //---------------------------------------------------------------------------//

}
