//#define DEBUG_Performance

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

namespace PDUServer
{
    public class ConnectionInfo
    {
        private enum CheckSystemIntegrityState { WORKED, ENABLE }
        #region private fields
        private object enterLockObject = new object();

        private Socket sSocket;
        private SortedList<Guid, ConnectionInfo> owner;
        private uint validitySessionPeriod;
        private uint enquireLinkPeriod;
        private Timer enquire_link_timer;
        private DateTime lastReciveCommandTime;
        CheckSystemIntegrityState checkSystemIntegrityState = CheckSystemIntegrityState.ENABLE;

        private uint lastSequence = 0;
        private ConnectionStates connectionState;
        private int timeout = 30000;

        Guid id;
        List<PDU> PDUResponseQueue = new List<PDU>();
        byte[] requestBuffer = new byte[1024 * 1024];
        uint positionStart = 0;
        uint positionFinish = 0;
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
                        pdu.CommandID = 0x00000015;
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
        private uint RequestLength
        {
            get
            {
                lock (enterLockObject)
                {
                    if (positionFinish >= positionStart)
                    {
                        return positionFinish - positionStart;
                    }
                    else
                    {
                        return (uint)requestBuffer.Length - positionStart + positionFinish;
                    }
                }
            }
        }

        public void AddRequest(byte[] data)
        {
            try
            {
                lastReciveCommandTime = DateTime.Now;
                uint dataLength = (uint)data.Length;
                uint requestBufferLength = 0;
#if DEBUG_Performance
                using (System.Diagnostics.PerformanceCounter performanceCounter = new System.Diagnostics.PerformanceCounter("AverageCounter64SampleCategory", "AverageCounter64Sample"))
                {
                    System.Diagnostics.CounterSample cs1;
                    System.Diagnostics.CounterSample cs2;
                    cs2 = performanceCounter.NextSample();
#endif
                requestBufferLength = (uint)requestBuffer.Length;
#if DEBUG_Performance
                    cs1 = cs2;
                    cs2 = performanceCounter.NextSample();
                    Double milliseconds = (Double)(cs2.CounterTimeStamp - cs1.CounterTimeStamp) / (Double)cs1.CounterFrequency * 1000;
                }
#endif
                lock (enterLockObject)
                {
                    if (requestBufferLength - RequestLength < dataLength)
                    {
                        throw new StackOverflowException("Произошло переполнение буффера requestBuffer");
                    }
                    uint old_positionFinish = positionFinish;
                    uint new_positionFinish = positionFinish + dataLength;
                    if (new_positionFinish > requestBufferLength)
                    {
                        new_positionFinish = new_positionFinish - requestBufferLength;
                    }
                    if (dataLength <= requestBufferLength - old_positionFinish)
                    {
#if DEBUG_Performance
                        using (System.Diagnostics.PerformanceCounter performanceCounter = new System.Diagnostics.PerformanceCounter("AverageCounter64SampleCategory", "AverageCounter64Sample"))
                        {
                            System.Diagnostics.CounterSample cs1;
                            System.Diagnostics.CounterSample cs2;
                            cs2 = performanceCounter.NextSample();
#endif
                        //Array.Copy(data, 0, requestBuffer, old_positionFinish, dataLength);
                        System.Buffer.BlockCopy(data, 0, requestBuffer, (int)old_positionFinish, (int)dataLength);
#if DEBUG_Performance
                            cs1 = cs2;
                            cs2 = performanceCounter.NextSample();
                            Double milliseconds = (Double)(cs2.CounterTimeStamp - cs1.CounterTimeStamp) / (Double)cs1.CounterFrequency * 1000;
                        }
#endif
                    }
                    else
                    {
                        Array.Copy(data, 0, requestBuffer, old_positionFinish, requestBufferLength - old_positionFinish);
                        Array.Copy(data, requestBufferLength - old_positionFinish, requestBuffer, 0, dataLength - (requestBufferLength - old_positionFinish));
                    }
                    positionFinish = new_positionFinish;

                    for (; ; )
                    {
                        uint requestLength = RequestLength;
                        if (requestLength >= 16)
                        {
                            uint FirstPacketLength = 0;
                            if (positionStart + 4 <= requestBufferLength)
                            {
                                Tools.ConvertArrayToUInt(requestBuffer, (int)positionStart, ref FirstPacketLength);
                            }
                            else
                            {
                                byte[] tmp = new byte[4];
                                Array.Copy(requestBuffer, positionStart, tmp, 0, requestBufferLength - positionStart);
                                Array.Copy(requestBuffer, 0, tmp, requestBufferLength - positionStart, 4 - (requestBufferLength - positionStart));
                                Tools.ConvertArrayToUInt(tmp, 0, ref FirstPacketLength);
                            }
                            if (FirstPacketLength <= requestLength)
                            {
                                byte[] packet = new byte[FirstPacketLength];
                                if (positionStart + FirstPacketLength < requestBufferLength)
                                {
                                    Array.Copy(requestBuffer, positionStart, packet, 0, FirstPacketLength);
                                    positionStart += FirstPacketLength;
                                }
                                else
                                {
                                    Array.Copy(requestBuffer, positionStart, packet, 0, requestBufferLength - positionStart);
                                    Array.Copy(requestBuffer, 0, packet, requestBufferLength - positionStart, FirstPacketLength - (requestBufferLength - positionStart));
                                    positionStart = FirstPacketLength - (requestBufferLength - positionStart);
                                }
                                Task doRequestWork = new Task(DoWorkRequest, packet);
                                doRequestWork.Start();
                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }
            }
            catch (Exception exc)
            {
                Logger.Log.ErrorFormat("Ошибка AddRequest клиент={0} address={1}", Id, RemoteEndPoint, exc);
            }
        }
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

                byte[] mid = new byte[] { 0x30, 0x31, 0x32, 0x33, 0x00 };
                PDU RespData = null;
                switch (Command)
                {
                    case 0x80000000://generic_nack
                        #region generic_nack
                        try
                        {
                            Logger.Log.Debug("DoWorkRequest:generic_nack");
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
                            Logger.Log.Fatal("Неизвестная ошибка в case 0x80000000 ReceiveCallback | ", exc);
                        }
                        #endregion
                        break;
                    case 0x00000009://bind_transceiver
                        #region bind_transceiver
                        try
                        {
                            Logger.Log.Debug("DoWorkRequest:bind_transceiver");
                            PDUBindTransceiver pt = new PDUBindTransceiver(packet);

                            PDUConfigSection sec = (PDUConfigSection)ConfigurationManager.GetSection("PDUConfig");

                            bool isCompleted = false;
                            UserCfgClass uc = sec.Server.Users[pt.SystemID];
                            if (uc != null)
                            {
                                if (uc.Password.Equals(pt.Password))
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
                            Logger.Log.Fatal("Неизвестная ошибка в case 0x00000009 ReceiveCallback | ", exc);
                        }
                        #endregion
                        break;
                    case 0x00000015://enquire_link
                        #region enquire_link
                        try
                        {
                            Logger.Log.Debug("DoWorkRequest:enquire_link");
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
                            Logger.Log.Fatal("Неизвестная ошибка в case 0x00000015 ReceiveCallback | ", exc);
                        }
                        #endregion
                        break;
                    case 0x80000015://enquire_link_response
                        #region enquire_link_response
                        try
                        {
                            Logger.Log.Debug("DoWorkRequest:enquire_link_response");
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
                            Logger.Log.Fatal("Неизвестная ошибка в case 0x00000015 ReceiveCallback | ", exc);
                        }
                        #endregion
                        break;
                    case 0x00000003://invoke
                        #region invoke
                        try
                        {
                            Logger.Log.Debug("DoWorkRequest:invoke");
                            PDUInvoke pi = new PDUInvoke(packet);
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
                                if (pi.IsInstance == 1)
                                {
                                    instance = instanceType.GetConstructor(new Type[] { }).Invoke(new object[] { });
                                }
                                MemberInfo mi = instanceType.GetMethod(pi.Method, pi.BindingFlags);
                                object[] aaa = pi.Arguments;

                                Type returnType = ((MethodInfo)mi).ReturnType;

                                object data = null;
                                Exception invokationException = null;
                                try
                                {
                                    data = instanceType.InvokeMember(pi.Method, BindingFlags.InvokeMethod | pi.BindingFlags, null, instance, aaa);
                                    // создадим ответ и добавим в очередь обработки
                                    RespData = new PDUInvokeResp(0, pi.Sequence + 1, returnType.FullName, data, null);
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
                            Logger.Log.Fatal("Неизвестная ошибка в case 0x00000003 ReceiveCallback | ", exc);
                        }
                        #endregion
                        break;
                    case 0x00000013://invokeByName
                        #region invokeByName
                        try
                        {
                            Logger.Log.Debug("DoWorkRequest:invokeByName");
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

                                object[] aaa = pi_bn.Arguments;

                                InvokeMethodsContainer inst = InvokeMethodsContainer.Instance;

                                InvokeMethodInfo info = InvokeMethodsContainer.Instance[pi_bn.InvokeName];

                                object data = null;
                                Exception invokationException = null;
                                try
                                {
                                    data = info.InstanceType.InvokeMember(
                                        info.MethodName, 
                                        BindingFlags.InvokeMethod | BindingFlags.DeclaredOnly | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static, 
                                        null, 
                                        info.Instance, 
                                        aaa
                                    );
                                    // создадим ответ и добавим в очередь обработки
                                    RespData = new PDUInvokeResp(0, pi_bn.Sequence + 1, info.ReturnType.FullName, data, null);
                                }
                                catch (TargetInvocationException ex)
                                {
                                    // создадим ответ и добавим в очередь обработки
                                    invokationException = ex.InnerException;
                                }

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
                            Logger.Log.Fatal("Неизвестная ошибка в case 0x00000003 ReceiveCallback | ", exc);
                        }
                        #endregion
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
