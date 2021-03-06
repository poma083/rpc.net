﻿//#define DEBUG_Performance

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using PDUDatas;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using System.Configuration;
using System.Security.Cryptography.X509Certificates;

namespace PDUServer
{
    public class ConnectionInfo
    {
        private enum CheckSystemIntegrityState { WORKED, ENABLE }
        #region private fields
        private object enterLockObject = new object();
        UserCfgClass userCfg;

        private Socket sSocket;
        private SortedList<Guid, ConnectionInfo> owner;
        private HashSet<string> subscribes = new HashSet<string>(); 
        private uint validitySessionPeriod;
        private uint enquireLinkPeriod;
        private Timer enquire_link_timer;
        private DateTime lastReciveCommandTime;
        CheckSystemIntegrityState checkSystemIntegrityState = CheckSystemIntegrityState.ENABLE;

        private uint lastSequence = 0;
        private ConnectionStates connectionState;
        private int timeout = 30000;

        Guid id;
        String systemId;
        String clientName;
        List<PDU> PDUResponseQueue = new List<PDU>();

        RingBuffer ringBuffer = new RingBuffer();
        //byte[] requestBuffer = new byte[1024];
        //int positionStart = -1;
        //uint positionFinish = 0;
        #endregion

        public ConnectionInfo(SortedList<Guid, ConnectionInfo> _owner, Socket s, uint EnquireLinkPeriod)
        {
            id = Guid.NewGuid();
            owner = _owner;
            Buffer = new byte[8192];
            sSocket = s;

            validitySessionPeriod = 3 * EnquireLinkPeriod;
            enquireLinkPeriod = EnquireLinkPeriod;

            // формирует запрос enquire_link проверяющий живо ли соединение
            TimerCallback timerDelegate = new TimerCallback(checkSystemIntegrity);
            enquire_link_timer = new Timer(timerDelegate, null, enquireLinkPeriod, enquireLinkPeriod);
        }
        private void checkSystemIntegrity(Object state)
        {
            if (checkSystemIntegrityState == CheckSystemIntegrityState.WORKED)
            {
                return;
            }
            lock (enterLockObject)
            {
                if (checkSystemIntegrityState == CheckSystemIntegrityState.WORKED)
                {
                    return;
                }
                checkSystemIntegrityState = CheckSystemIntegrityState.WORKED;
            }
            try
            {
                DateTime local_dt = DateTime.Now;
                if (lastReciveCommandTime.AddMilliseconds(enquireLinkPeriod) < local_dt)// значит пора опросит соединение
                {
                    if (lastReciveCommandTime.AddMilliseconds(validitySessionPeriod) < local_dt)
                    {
                        //закрываем сессию
                        lock (enterLockObject)
                        {
                            if (sSocket != null)
                            {
                                Logger.Log.Warn("Клиент c address=\"" + this.sSocket.RemoteEndPoint.ToString() + "\" просрочил ответы запросов enquire_link соединение будет закрыто!");
                            }
                        }
                        this.CloseConnection();
                    }
                    else
                    {
                        //посылаем enquireLink
                        PDU pdu = new PDU();
                        lock (enterLockObject)
                        {
                            unchecked
                            {
                                this.lastSequence++;
                            }
                            pdu.Sequence = this.lastSequence;
                        }
                        pdu.CommandState = 0;
                        //pdu.Lenght = 16;-
                        pdu.CommandID = MessageType.EnquireLink;
                        pdu.CommandState = 0x0000000D;

                        if (pdu != null)
                        {
                            this.AddPDUToResponseQueue(pdu);
                        }
                        lock (enterLockObject)
                        {
                            if (sSocket != null)
                            {
                                Logger.Log.Debug("Посылаем запрос enquire_link клиенту " + this.sSocket.RemoteEndPoint.ToString());
                            }
                        }
                        DoWorkResponses();
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Fatal(" checkSystemIntegrity | " + ex.ToString());
                throw;
            }
            finally
            {
                if (checkSystemIntegrityState == CheckSystemIntegrityState.WORKED)
                {
                    checkSystemIntegrityState = CheckSystemIntegrityState.ENABLE;
                }
            }

        }//checkSystemIntegrity
        public Guid Id
        {
            get
            {
                lock (enterLockObject)
                {
                    return id;
                }
            }
        }
        public byte[] Buffer { get; set; }
        public ConnectionStates ConnectionState
        {
            get
            {
                lock (enterLockObject)
                {
                    return connectionState;
                }
            }
            set
            {
                lock (enterLockObject)
                {
                    connectionState = value;
                }
            }
        }
        public System.Net.EndPoint RemoteEndPoint
        {
            get
            {
                lock (enterLockObject)
                {
                    if (sSocket != null)
                    {
                        return this.sSocket.RemoteEndPoint;
                    }
                }
                return null;
            }
        }
        public void CloseConnection()
        {
            this.enquire_link_timer.Dispose();
            lock (enterLockObject)
            {
                if (sSocket != null)
                {
                    sSocket.Close();
                    sSocket.Dispose();
                    sSocket = null;
                }
            }
            lock (owner)
            {
                owner.Remove(this.Id);
            }
        }
        public IAsyncResult BeginReceive(int offset, AsyncCallback callback)
        {
            lock (enterLockObject)
            {
                if (sSocket != null)
                {
                    return this.sSocket.BeginReceive(this.Buffer, offset, this.Buffer.Length, SocketFlags.None, callback, this);
                }
                return null;
            }
        }
        public int EndReceive(IAsyncResult AsyncResult)
        {
            lock (enterLockObject)
            {
                if (sSocket != null)
                {
                    return sSocket.EndReceive(AsyncResult);
                }
                return -1;
            }
        }
        private IAsyncResult Send(byte[] data)
        {
            lock (enterLockObject)
            {
                if (sSocket != null)
                {
                    if (sSocket.Connected)
                    {
                        return sSocket.BeginSend(data, 0, data.Length, SocketFlags.None, SendCallback, null);
                    }
                }
            }
            return null;
        }
        private void SendCallback(IAsyncResult result)
        {

        }
        public void SetTimeout(int _timeout)
        {
            lock (enterLockObject)
            {
                this.timeout = _timeout;
            }
        }
        
        #region ringBuffer
        //private uint GetLenPacket(uint start)
        //{
        //    uint result = 0;
        //    lock (enterLockObject)
        //    {
        //        if (start + 4 <= requestBuffer.Length)
        //        {
        //            Tools.ConvertArrayToUInt(requestBuffer, (int)start, ref result);
        //        }
        //        else
        //        {
        //            byte[] tmp = new byte[4];
        //            Array.Copy(requestBuffer, start, tmp, 0, requestBuffer.Length - start);
        //            Array.Copy(requestBuffer, 0, tmp, requestBuffer.Length - start, 4 - (requestBuffer.Length - start));
        //            Tools.ConvertArrayToUInt(tmp, 0, ref result);
        //        }
        //    }
        //    return result;
        //}
        //private uint RequestLength
        //{
        //    get
        //    {
        //        lock (enterLockObject)
        //        {
        //            if (positionStart < 0)
        //            {
        //                return 0;
        //            }
        //            if (positionFinish > positionStart)
        //            {
        //                return positionFinish - (uint)positionStart;
        //            }
        //            else if (positionFinish < positionStart)
        //            {
        //                return (uint)requestBuffer.Length - (uint)positionStart + positionFinish;
        //            }
        //            else
        //            {
        //                return (uint)requestBuffer.Length;
        //            }
        //        }
        //    }
        //}
        //public void AddRequest(byte[] data)
        //{
        //    try
        //    {
        //        lastReciveCommandTime = DateTime.Now;
        //        uint dataLength = (uint)data.Length;
        //        lock (enterLockObject)
        //        {
        //            uint requestBufferLength = (uint)requestBuffer.Length;
        //            uint rl = RequestLength;
        //            #region если пришедшие данные не влазят в свободное пространство кольцевого буфера
        //            // то выделим под кольцевой буффер дополнительное пространство
        //            if (requestBufferLength < rl + dataLength) // не влазят
        //            {
        //                Array.Resize(ref requestBuffer, (int)requestBufferLength + (int)dataLength);
        //                if (positionStart >= positionFinish)
        //                {
        //                    Array.Copy(requestBuffer, positionStart, requestBuffer, positionStart + dataLength, requestBufferLength - positionStart);
        //                    positionStart += (int)dataLength;
        //                }
        //                requestBufferLength = (uint)requestBuffer.Length;
        //                Logger.Log.WarnFormat("Перераспределение размера буфера. Новый размер буфера \"{0}\"", requestBufferLength);

        //            }
        //            #endregion
        //            #region переносим пришедшие данные в наш кольцевой буфер
        //            if (positionFinish > positionStart)
        //            {
        //                if (positionFinish + dataLength <= requestBufferLength)
        //                {
        //                    System.Buffer.BlockCopy(data, 0, requestBuffer, (int)positionFinish, (int)dataLength);
        //                }
        //                else
        //                {
        //                    Array.Copy(data, 0, requestBuffer, positionFinish, requestBufferLength - positionFinish);
        //                    Array.Copy(data, requestBufferLength - positionFinish, requestBuffer, 0, dataLength - (requestBufferLength - positionFinish));
        //                }
        //                if (positionStart < 0) //то-есть буфер был пуст
        //                {
        //                    positionStart = (int)positionFinish;
        //                }
        //                positionFinish += dataLength;
        //                if (positionFinish > requestBufferLength)
        //                {
        //                    positionFinish -= requestBufferLength;
        //                }
        //            }
        //            else if (positionFinish < positionStart)
        //            {
        //                System.Buffer.BlockCopy(data, 0, requestBuffer, (int)positionFinish, (int)dataLength);
        //                positionFinish += dataLength;
        //            }
        //            else
        //            {
        //                throw new StackOverflowException("Произошло переполнение буффера requestBuffer");
        //            }
        //            #endregion
        //            #region запускаем цикл обработки пакетов находящихся в кольцевом буффере
        //            for (; ; )
        //            {
        //                uint requestLength = RequestLength;
        //                if (requestLength >= 16)
        //                {
        //                    uint FirstPacketLength = GetLenPacket((uint)positionStart);
        //                    if (FirstPacketLength <= requestLength)
        //                    {
        //                        byte[] packet = new byte[FirstPacketLength];
        //                        if (positionStart + FirstPacketLength <= requestBufferLength)
        //                        {
        //                            Array.Copy(requestBuffer, positionStart, packet, 0, FirstPacketLength);
        //                            positionStart += (int)FirstPacketLength;
        //                        }
        //                        else
        //                        {
        //                            Array.Copy(requestBuffer, positionStart, packet, 0, requestBufferLength - positionStart);
        //                            Array.Copy(requestBuffer, 0, packet, requestBufferLength - positionStart, FirstPacketLength - (requestBufferLength - positionStart));
        //                            positionStart = (int)FirstPacketLength - ((int)requestBufferLength - positionStart);
        //                        }
        //                        if (positionStart == positionFinish)//то-есть буфер пуст
        //                        {
        //                            positionStart = -1;
        //                        }
        //                        Task doRequestWork = new Task(DoWorkRequest, packet);
        //                        doRequestWork.Start();
        //                    }
        //                    else
        //                    {
        //                        break;
        //                    }
        //                }
        //                else
        //                {
        //                    break;
        //                }
        //            }
        //            #endregion
        //        }
        //    }
        //    catch (Exception exc)
        //    {
        //        Logger.Log.ErrorFormat("Ошибка AddRequest клиент={0} address={1}", Id, RemoteEndPoint, exc);
        //    }
        //}
        public void AddRequest(byte[] data)
        {
            lastReciveCommandTime = DateTime.Now;
            ringBuffer.Add(data, DoWorkRequest);
        }
        #endregion

        private int AddPDUToResponseQueue(PDU data)
        {
            lock (PDUResponseQueue)
            {
                PDUResponseQueue.Add(data);
                return PDUResponseQueue.Count;
            }
        }

        private void DoWorkRequest(object state)
        {
            try
            {
                byte[] packet = state as byte[];
                if (packet == null)
                {
                    throw new ArgumentException("Буффер пакета не должен быть null");
                }
                uint Command = 0;
                Tools.ConvertArrayToUInt(packet, 4, ref Command);
                Logger.Log.DebugFormat("DoWorkRequest:{0}", Command);
                byte[] mid = new byte[] { 0x30, 0x31, 0x32, 0x33, 0x00 };
                PDU RespData = null;
                MessageType cmd = (MessageType)Command;
                switch (cmd)
                {
                    case MessageType.GenericNack://generic_nack
                        #region generic_nack
                        try
                        {
                            PDUGenericNack pgn = new PDUGenericNack(packet);
                            if (evGenericNack != null)
                            {
                                try
                                {
                                    evGenericNack(pgn, this);
                                }
                                catch (Exception exc)
                                {
                                    Logger.Log.Fatal("Ошибка при пользовательской обработке evGenericNack ", exc);
                                }
                            }
                        }
                        catch (Exception exc)
                        {
                            Logger.Log.FatalFormat("{0}: Неизвестная ошибка в case {1} ReceiveCallback | {2}", clientName, cmd.ToString("g"), exc);
                        }
                        #endregion
                        break;
                    case MessageType.BindTransceiver://bind_transceiver
                        #region bind_transceiver
                        try
                        {
                            PDUBindTransceiver pt = new PDUBindTransceiver(packet);
                            systemId = pt.SystemID;
                            clientName = pt.ConfigurationName;

                            PDUConfigSection sec = (PDUConfigSection)ConfigurationManager.GetSection("PDUConfig");

                            bool isCompleted = false;
                            userCfg = sec.Server.Users[pt.SystemID];
                            if (userCfg != null)
                            {
                                if (userCfg.Password.Equals(pt.Password))
                                {
                                    isCompleted = true;
                                }
                            }

                            pt.CommandState = isCompleted ? (uint)0 : (uint)3;

                            SetTimeout((int)pt.Timeout);
                            if (evConnect != null)
                            {
                                try
                                {
                                    evConnect(pt, this);
                                }
                                catch (Exception exc)
                                {
                                    Logger.Log.Fatal("Ошибка при пользовательской обработке evConnect ", exc);
                                }
                            }
                            if (pt.CommandState == 0)
                            {
                                ConnectionState = ConnectionStates.BINDED;
                            }

                            // создадим ответ и добавим в очередь обработки
                            RespData = new PDUBindTransceiverResp(pt.CommandState, pt.Sequence, pt.SystemID);
                            if (evConnectCompleted != null)
                            {
                                try
                                {
                                    evConnectCompleted(RespData, this);
                                }
                                catch (Exception exc)
                                {
                                    Logger.Log.Fatal("Ошибка при пользовательской обработке evConnected ", exc);
                                }
                            }
                            if (RespData != null)
                            {
                                AddPDUToResponseQueue(RespData);
                            }
                        }
                        catch (Exception exc)
                        {
                            Logger.Log.FatalFormat("{0}: Неизвестная ошибка в case {1} ReceiveCallback | {2}", clientName, cmd.ToString("g"), exc);
                        }
                        #endregion
                        break;
                    case MessageType.EnquireLink://enquire_link
                        #region enquire_link
                        try
                        {
                            PDUEnquireLink pel = new PDUEnquireLink(packet);

                            if (evEnquireLink != null)
                            {
                                try
                                {
                                    evEnquireLink(pel, this);
                                }
                                catch (Exception exc)
                                {
                                    Logger.Log.Fatal("Ошибка при пользовательской обработке evEnquireLink ", exc);
                                }
                            }

                            // создадим ответ и добавим в очередь обработки
                            RespData = new PDUEnquireLinkResp(pel.CommandState, pel.Sequence);

                            if (evEnquireLinkCompleted != null)
                            {
                                try
                                {
                                    evEnquireLinkCompleted(RespData, this);
                                }
                                catch (Exception exc)
                                {
                                    Logger.Log.Fatal("Ошибка при пользовательской обработке evEnquireLinkCompleted ", exc);
                                }
                            }
                            if (RespData != null)
                            {
                                AddPDUToResponseQueue(RespData);
                            }
                        }
                        catch (Exception exc)
                        {
                            Logger.Log.FatalFormat("{0}: Неизвестная ошибка в case {1} ReceiveCallback | {2}", clientName, cmd.ToString("g"), exc);
                        }
                        #endregion
                        break;
                    case MessageType.EnquireLinkResp://enquire_link_response
                        #region enquire_link_response
                        try
                        {
                            HeadPDU head = new HeadPDU(packet);
                            PDUEnquireLinkResp pelr = new PDUEnquireLinkResp(head.commandstate, head.sequence);

                            if (evEnquireLinkResp != null)
                            {
                                try
                                {
                                    evEnquireLinkResp(pelr, this);
                                }
                                catch (Exception exc)
                                {
                                    Logger.Log.Fatal("Ошибка при пользовательской обработке evEnquireLinkResp ", exc);
                                }
                            }

                            if (evEnquireLinkRespCompleted != null)
                            {
                                try
                                {
                                    evEnquireLinkRespCompleted(RespData, this);
                                }
                                catch (Exception exc)
                                {
                                    Logger.Log.Fatal("Ошибка при пользовательской обработке evEnquireLinkRespCompleted ", exc);
                                }
                            }
                        }
                        catch (Exception exc)
                        {
                            Logger.Log.FatalFormat("{0}: Неизвестная ошибка в case {1} ReceiveCallback | {2}", clientName, cmd.ToString("g"), exc);
                        }
                        #endregion
                        break;
                    case MessageType.Invoke://invoke
                        #region invoke
                        try
                        {
                            PDUInvoke pi = new PDUInvoke(packet);
                            string assName;
                            string typeName;
                            byte isInstance;
                            string method;
                            BindingFlags bindingFlags;
                            object[] arguments;
                            pi.GetData(out assName, out typeName, out isInstance, out method, out bindingFlags, out arguments);
                            Logger.Log.DebugFormat("\"{0}\": {1}->{2}->{3}", clientName, assName, typeName, method);
                            if (ConnectionState == ConnectionStates.BINDED)
                            {
                                if (evInvoke != null)
                                {
                                    try
                                    {
                                        evInvoke(pi, this);
                                    }
                                    catch (Exception exc)
                                    {
                                        Logger.Log.Fatal("Ошибка при пользовательской обработке evInvoke ", exc);
                                    }
                                }

                                Assembly ass = null;// pi.Assembly;
                                Type instanceType = null;// pi.InstanceType;
                                if (pi.InstanceType.IsInterface)
                                {
                                    Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                                    foreach (Assembly a in assemblies)
                                    {
                                        Type[] types = a.GetTypes();
                                        foreach (Type t in types)
                                        {
                                            Type interfaceType = t.GetInterfaces().Where(it => it.FullName == pi.InstanceType.FullName).FirstOrDefault();
                                            if (interfaceType == null)
                                            {
                                                continue;
                                            }
                                            instanceType = t;
                                            break;
                                        }
                                        if (instanceType != null)
                                        {
                                            ass = a;
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    ass = pi.Assembly;
                                    instanceType = pi.InstanceType;
                                }
                                object instance = null;
                                if (isInstance == 1)
                                {
                                    instance = instanceType.GetConstructor(new Type[] { }).Invoke(new object[] { });
                                }
                                Type[] argumentTypes = new Type[arguments.Length];
                                for (int i = 0; i < arguments.Length; i++ )
                                {
                                    argumentTypes[i] = arguments[i].GetType();
                                }
                                MethodInfo mi = instanceType.GetMethod(method, bindingFlags, null, argumentTypes, null);
                                Type returnType = mi.ReturnType;

                                object data = null;
                                Exception invokationException = null;
                                try
                                {
                                    data = mi.Invoke(instance, arguments);
                                    //data = instanceType.InvokeMember(method, BindingFlags.InvokeMethod | bindingFlags, null, instance, arguments);
                                    InvokeEventsContainer.Instance.Set(mi, argumentTypes);
                                }
                                catch (TargetInvocationException ex)
                                {
                                    // создадим ответ и добавим в очередь обработки
                                    invokationException = ex.InnerException;
                                }
                                RespData = new PDUInvokeResp(0, pi.Sequence, returnType.FullName, data, invokationException);
                            }
                            else
                            {
                                RespData = new PDUInvokeResp(0, pi.Sequence, typeof(void).FullName, null, new Exception("Клиент не прошёл авторизацию"));
                            }

                            if (evInvokeCompleted != null)
                            {
                                try
                                {
                                    evInvokeCompleted(RespData, this);
                                }
                                catch (Exception exc)
                                {
                                    Logger.Log.Fatal("Ошибка при пользовательской обработке evInvokeCompleted ", exc);
                                }
                            }
                            if (RespData != null)
                            {
                                AddPDUToResponseQueue(RespData);
                            }
                        }
                        catch (Exception exc)
                        {
                            Logger.Log.FatalFormat("{0}: Неизвестная ошибка в case {1} ReceiveCallback | {2}", clientName, cmd.ToString("g"), exc);
                        }
                        #endregion
                        break;
                    case MessageType.InvokeByName://invokeByName
                        #region invokeByName
                        try
                        {
                            PDUInvokeByName pi_bn = new PDUInvokeByName(packet);
                            if (ConnectionState == ConnectionStates.BINDED)
                            {
                                if (evInvokeByName != null)
                                {
                                    try
                                    {
                                        evInvokeByName(pi_bn, this);
                                    }
                                    catch (Exception exc)
                                    {
                                        Logger.Log.Fatal("Ошибка при пользовательской обработке evInvokeByName ", exc);
                                    }
                                }

                                object[] arguments = null;
                                string alias = null;
                                pi_bn.GetData(out alias, out arguments);

                                object data = null;
                                Exception invokationException = null;
                                InvokeMethodInfo info = InvokeMethodsContainer.Instance[alias];
                                if (info != null)
                                {
                                    try
                                    {
                                        data = info.MethodInfo.Invoke(info.Instance, arguments);
                                        //data = info.InstanceType.InvokeMember(
                                        //    info.MethodName,
                                        //    BindingFlags.InvokeMethod | BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static,
                                        //    null,
                                        //    info.Instance,
                                        //    arguments
                                        //);
                                        InvokeEventsContainer.Instance.Set(alias);
                                    }
                                    catch (TargetInvocationException ex)
                                    {
                                        // создадим ответ и добавим в очередь обработки
                                        invokationException = ex.InnerException;
                                    }
                                }
                                else
                                {
                                    invokationException = new MissingMethodException("invoke_by_name",pi_bn.InvokeName);
                                }

                                // создадим ответ и добавим в очередь обработки
                                RespData = new PDUInvokeResp(0, pi_bn.Sequence, info.ReturnType.FullName, data, invokationException);
                            }
                            else
                            {
                                RespData = new PDUInvokeResp(0, pi_bn.Sequence, typeof(void).FullName, null, new Exception("Клиент не прошёл авторизацию"));
                            }

                            if (evInvokeByNameCompleted != null)
                            {
                                try
                                {
                                    evInvokeByNameCompleted(RespData, this);
                                }
                                catch (Exception exc)
                                {
                                    Logger.Log.Fatal("Ошибка при пользовательской обработке evInvokeByNameCompleted ", exc);
                                }
                            }
                            if (RespData != null)
                            {
                                AddPDUToResponseQueue(RespData);
                            }
                        }
                        catch (Exception exc)
                        {
                            Logger.Log.FatalFormat("{0}: Неизвестная ошибка в case {1} ReceiveCallback | {2}", clientName, cmd.ToString("g"), exc);
                        }
                        #endregion
                        break;
                    case MessageType.InvokeSecureByName://invokeSecureByName
                        #region invokeSecureByName
                        try
                        {
                            PDUInvokeSecureByName pis_bn = new PDUInvokeSecureByName(packet);

                            Exception invokationException = null;
                            if (ConnectionState == ConnectionStates.BINDED)
                            {
                                if (evInvokeByName != null)
                                {
                                    try
                                    {
                                        evInvokeByName(pis_bn, this);
                                    }
                                    catch (Exception exc)
                                    {
                                        Logger.Log.FatalFormat("\"{0}\": Ошибка при пользовательской обработке evInvokeByName {1}", clientName, exc);
                                    }
                                }
                                InvokeMethodInfo info = null;
                                object data = null;
                                object[] arguments = null;
                                string alias = null;
                                byte[] clientPublicCertificateRaw = null;
                                X509Certificate2 clientPublicCertificate = null;
                                try
                                {
                                    pis_bn.GetData(out alias, out arguments, out clientPublicCertificateRaw,
                                        userCfg.ServerCertificate.StoreName, 
                                        userCfg.ServerCertificate.StoreLocation, 
                                        userCfg.ServerCertificate.Thumbprint);
                                    clientPublicCertificate = new X509Certificate2(clientPublicCertificateRaw);
                                    //SCZI.ValidateCertificate(clientPublicCertificate);
                                    info = InvokeMethodsContainer.Instance[alias];
                                }
                                catch (System.Security.Cryptography.CryptographicException cEx)
                                {
                                    invokationException = cEx;
                                }
                                if (invokationException == null)
                                {
                                    try
                                    {
                                        data = info.MethodInfo.Invoke(info.Instance, arguments);
                                        //data = info.InstanceType.InvokeMember(
                                        //    info.MethodName,
                                        //    BindingFlags.InvokeMethod | BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static,
                                        //    null,
                                        //    info.Instance,
                                        //    arguments
                                        //);
                                        InvokeEventsContainer.Instance.Set(alias);
                                    }
                                    catch (TargetInvocationException ex)
                                    {
                                        invokationException = ex.InnerException;
                                    }
                                }
                                // создадим ответ и добавим в очередь обработки
                                RespData = new PDUInvokeSecureResp(0, pis_bn.Sequence, info != null ? info.ReturnType.FullName : "", data, invokationException, clientPublicCertificate);
                            }
                            else
                            {
                                invokationException = new AccessViolationException(String.Format("\"{0}\": Клиент не прошёл авторизацию", clientName));
                                // создадим ответ и добавим в очередь обработки
                                RespData = new PDUInvokeResp(0, pis_bn.Sequence, "", null, invokationException);
                            }

                            if (evInvokeByNameCompleted != null)
                            {
                                try
                                {
                                    evInvokeByNameCompleted(RespData, this);
                                }
                                catch (Exception exc)
                                {
                                    Logger.Log.FatalFormat("\"{0}\": Ошибка при пользовательской обработке evInvokeByNameCompleted {1}", clientName, exc);
                                }
                            }
                            if (RespData != null)
                            {
                                AddPDUToResponseQueue(RespData);
                            }
                        }
                        catch (Exception exc)
                        {
                            Logger.Log.FatalFormat("\"{0}\": Неизвестная ошибка в case {1} ReceiveCallback | {2}", clientName, cmd, exc);
                        }
                        #endregion
                        break;
                    case MessageType.Wait://wait
                        #region wait
                        try
                        {
                            PDUWait pw = new PDUWait(packet);
                            string key;
                            WaitType waitType;
                            pw.GetData(out key, out waitType);
                            if (ConnectionState == ConnectionStates.BINDED)
                            {
                                //if (evInvoke != null)
                                //{
                                //    try
                                //    {
                                //        evInvoke(pi, this);
                                //    }
                                //    catch (Exception exc)
                                //    {
                                //        Logger.Log.Fatal("Ошибка при пользовательской обработке evInvoke ", exc);
                                //    }
                                //}
                                Exception invokationException = null;
                                try
                                {
                                    InvokeEvent @event = InvokeEventsContainer.Instance.Create(key);
                                    if(waitType == WaitType.WaitAll)
                                    {
                                        @event.WaitManual();
                                    }
                                    else
                                    {
                                        @event.WaitAuto();
                                    }
                                }
                                catch (TargetInvocationException ex)
                                {
                                    // создадим ответ и добавим в очередь обработки
                                    invokationException = ex.InnerException;
                                }
                                RespData = new PDUWaitResp(0, pw.Sequence);
                            }
                            else
                            {
                                RespData = new PDUWaitResp(3, pw.Sequence);
                            }

                            //if (evInvokeCompleted != null)
                            //{
                            //    try
                            //    {
                            //        evInvokeCompleted(RespData, this);
                            //    }
                            //    catch (Exception exc)
                            //    {
                            //        Logger.Log.Fatal("Ошибка при пользовательской обработке evInvokeCompleted ", exc);
                            //    }
                            //}
                            if (RespData != null)
                            {
                                AddPDUToResponseQueue(RespData);
                            }
                        }
                        catch (Exception exc)
                        {
                            Logger.Log.FatalFormat("{0}: Неизвестная ошибка в case {1} ReceiveCallback | {2}", clientName, cmd.ToString("g"), exc);
                        }
                        #endregion
                        break;
                    default :
                        Logger.Log.FatalFormat("{0}: Неизвестная команда {1}", clientName, cmd);
                        break;
                }

                int WorkiongResponsesCount = DoWorkResponses();
            }
            catch (Exception ex)
            {
                Logger.Log.ErrorFormat("Ошибка DoWorkRequest клиент={0} host={1}", Id, RemoteEndPoint, ex);
            }
        }
        private int DoWorkResponses()
        {
            try
            {
                int ResponseQueueCount = 0;
                PDU[] TemporaryQueue = null;
                lock (PDUResponseQueue)
                {
                    ResponseQueueCount = PDUResponseQueue.Count;
                    TemporaryQueue = PDUResponseQueue.ToArray();
                    this.PDUResponseQueue.Clear();
                }

                if (ResponseQueueCount > 0)
                {
                    int AnswerLength = 0;
                    int CurrentAnswerPosition = 0;
                    for (int i = 0; i < ResponseQueueCount; i++)
                    {
                        AnswerLength = AnswerLength + TemporaryQueue[i].Lenght;
                    }
                    byte[] Answer = new byte[AnswerLength];
                    for (int i = 0; i < ResponseQueueCount; i++)
                    {
                        Array.Copy(TemporaryQueue[i].AllData, 0, Answer, CurrentAnswerPosition, TemporaryQueue[i].Lenght);
                        CurrentAnswerPosition = CurrentAnswerPosition + TemporaryQueue[i].Lenght;
                    }

                    this.Send(Answer);
                }
                Logger.Log.Debug("__DoWorkResponses");
                return ResponseQueueCount;
            }
            catch (Exception ex)
            {
                Logger.Log.ErrorFormat("Ошибка DoWorkResponses клиент={0} host={1}", Id, RemoteEndPoint, ex);
            }
            return 0;
        }

        #region events
        //generic_nack
        public event BeforeEventHandler evGenericNack;
        public event AffterEventHandler evGenericNackCompleted;
        //bind_transceiver
        public event BeforeEventHandler evConnect;
        public event AffterEventHandler evConnectCompleted;
        //enquire_link
        public event BeforeEventHandler evEnquireLink;
        public event AffterEventHandler evEnquireLinkCompleted;
        //enquire_link_response
        public event BeforeEventHandler evEnquireLinkResp;
        public event AffterEventHandler evEnquireLinkRespCompleted;
        //invoke
        public event BeforeEventHandler evInvoke;
        public event AffterEventHandler evInvokeCompleted;
        //invokeByName
        public event BeforeEventHandler evInvokeByName;
        public event AffterEventHandler evInvokeByNameCompleted;
        #endregion
    }
}
