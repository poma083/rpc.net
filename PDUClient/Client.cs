using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using PDUDatas;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

namespace PDUClient
{
    public enum ClientState { NONE = 0, ENABLED = 1, DISABLED }
    public class Client
    {
        private class InvokeQueueItem : IDisposable
        {
            public PDU request;
            public PDUInvokeResp response;
            public clientCallback<PDUInvokeResp> callback;
            public EventWaitHandle waitEvent = new EventWaitHandle(false, EventResetMode.ManualReset);

            #region IDisposable Members

            public void Dispose()
            {
                if (waitEvent != null)
                {
                    waitEvent.Close();
                    waitEvent.Dispose();
                }
            }

            #endregion
        }
        #region private fields
        private static object lockObject = new object();
        private Socket _socket;
        private ClientCfgClass config;
        private uint sequence = 0;
        private byte[] buffer = new byte[8192];

        RingBuffer ringBuffer = new RingBuffer(); 
        //private byte[] receivedBuffer = new byte[1024 * 1024];
        //private int positionStart = -1;
        //private uint positionFinish = 0;

        private Timer generic_nack_timer = null;

        private Dictionary<uint, InvokeQueueItem> invokeQueue = new Dictionary<uint, InvokeQueueItem>();

        private ClientState state = ClientState.NONE;
        #endregion

        public override string ToString()
        {
            return base.ToString();
            StringBuilder sb = new StringBuilder("{");

            sb.Append("\"State\"=\"");
            sb.Append(state.ToString("g"));
            sb.Append("\",");

            sb.Append("\"sequence\"=\"");
            sb.Append(sequence.ToString());
            sb.Append("\",");

            sb.Append("\"receivedBufferLength\"=\"");
            sb.Append(ringBuffer.BufferLength.ToString());
            sb.Append("\",");
            if (_socket != null)
            {
                if (_socket.RemoteEndPoint != null)
                {
                    sb.Append("\"_socketAddress\"=\"");
                    sb.Append(_socket.RemoteEndPoint.ToString());
                    sb.Append("\",");
                }

                sb.Append("\"_socketIsConnected\"=\"");
                sb.Append(_socket.Connected.ToString());
                sb.Append("\",");
            }
            else
            {
                sb.Append("\"_socket\"=\"null\",");
            }

            sb.Append("\"config\"=");
            sb.Append(config.ToString());

            sb.Append("}");
            return sb.ToString();
        }
        public ClientState State
        {
            get
            {
                lock (lockObject)
                {
                    return state;
                }
            }
            set
            {
                lock (lockObject)
                {
                    state = value;
                }
            }
        }

        private void sendGenericNack(Object state)
        {
            SocketError se = SocketError.SocketError;
            PDUGenericNack pgn = null;
            lock (lockObject)
            {
                unchecked
                {
                    sequence++;
                }
                pgn = new PDUGenericNack(0, sequence);
            }
            try
            {
                int recv = Send(pgn.AllData, 0, pgn.Lenght, SocketFlags.None, out se);
            }
            //catch (SocketException e)
            //{
            //    Logger.Log.ErrorFormat("Ошибка отправки данных SocketError в sendGenericNack, SocketError={0}, Error = {1}", se, e);
            //    Disonnect();
            //}
            catch (Exception e)
            {
                Logger.Log.Error("Ошибка при отправке данных в sendGenericNack", e);
            }
        }

        public Client(ClientCfgClass cfg)
        {
            Logger.Log.Debug("sdfhbvg gf juyegfjy egyuf u");
            config = cfg;
            IPAddress adr = IPAddress.Any;
            IPAddress.TryParse(config.Host, out adr);
            IPEndPoint myEndpoint = new IPEndPoint(adr, config.Port);
            // Start();

            TimerCallback timerDelegate = new TimerCallback(sendGenericNack);
            generic_nack_timer = new Timer(timerDelegate, null, config.GenericNackPeriod, config.GenericNackPeriod);
        }
        public PDUBindTransceiver Connect()
        {
            try
            {
                lock (lockObject)
                {
                    if (_socket == null)
                        Start();

                    if (!_socket.Connected)
                    {
                        _socket.Connect(config.Host, config.Port);
                    }
                    if (_socket.Connected)
                    {
                        SocketError se = new SocketError();
                        PDUBindTransceiver pbt = null;
                        lock (lockObject)
                        {
                            unchecked
                            {
                                sequence++;
                            }
                            pbt = new PDUBindTransceiver(0, sequence, config.Login, config.Password, config.Timeout, config.Name);
                        }
                        string sss = pbt.SystemID;
                        int recv = Send(pbt.AllData, 0, pbt.Lenght, SocketFlags.None, out se);

                        if (_socket != null)
                        {
                            _socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), this);
                        }
                        System.Threading.Thread.Sleep(100);
                        return pbt;
                    }
                }
            }
            catch (SocketException e)
            {
                Logger.Log.Error("Ошибка в Connect", e);
                Disonnect();
            }
            catch (Exception e)
            {
                Logger.Log.Error("Ошибка в Connect", e);
                throw;
            }
            return null;
        }
        public void Disonnect()
        {
            List<InvokeQueueItem> tmp = new List<InvokeQueueItem>();
            if (_socket == null) return;
            lock (lockObject)
            {
                if (_socket == null) return;
                if (_socket.Connected)
                {
                    _socket.Shutdown(SocketShutdown.Both);
                    _socket.Disconnect(true);
                }
                _socket.Dispose();
                if (invokeQueue != null)
                {
                    tmp.AddRange(invokeQueue.Values);
                }
                _socket = null;
                state = ClientState.DISABLED;
            }
            foreach (InvokeQueueItem item in tmp)
            {
                item.waitEvent.Dispose();
            }
            lock (lockObject)
            {
                invokeQueue.Clear();
            }
        }
        private int Send(byte[] buffer, int offset, int size, SocketFlags socketFlags, out SocketError errorCode)
        {
            int result = -1;
            lock (lockObject)
            {
                if (_socket == null)
                {
                    Connect();
                }

                if (_socket == null)
                {
                    Logger.Log.Error("_socket is null after Connect!");
                    errorCode = SocketError.SocketError;
                    return -1;
                }
                try
                {
                    result = _socket.Send(buffer, offset, size, socketFlags, out errorCode);
                    state = ClientState.ENABLED;
                    //Logger.Log.ErrorFormat("sended from cient={0}", ToString());
                }
                catch (SocketException)
                {
                    Logger.Log.ErrorFormat("send error from cient={0}", ToString());
                    Disonnect();
                    throw;
                }
                return result;
            }
        }

        public PDUInvoke CreateInvokeData(Assembly ass, Type instanceType, MethodInfo mi, params object[] arguments)
        {
            BindingFlags bf = (mi.IsPublic ? BindingFlags.Public : BindingFlags.NonPublic)
                            | (mi.IsStatic ? BindingFlags.Static : BindingFlags.Instance);
            bool callFromInstance = mi.IsStatic ? false : true;
            PDUInvoke pi = null;
            lock (lockObject)
            {
                unchecked
                {
                    sequence++;
                }
                pi = new PDUInvoke(0, sequence, ass.FullName, instanceType.FullName, callFromInstance ? (byte)1 : (byte)0, mi.Name, bf, arguments);
            }
            return pi;
        }
        public PDUInvokeByName CreateInvokeByName(String invokeName, params object[] arguments)
        {
            PDUInvokeByName pi = null;
            lock (lockObject)
            {
                unchecked
                {
                    sequence++;
                }
                pi = new PDUInvokeByName(0, sequence, invokeName, arguments);
            }
            return pi;
        }

        private SocketError InvokeAsync(PDUInvoke data)
        {
            SocketError se = new SocketError();
            int recv = Send(data.AllData, 0, data.Lenght, SocketFlags.None, out se);
            return se;
        }
        private SocketError InvokeAsync(PDUInvokeByName data)
        {
            SocketError se = new SocketError();
            int recv = Send(data.AllData, 0, data.Lenght, SocketFlags.None, out se);
            return se;
        }
        public void InvokeAsync(PDUInvoke data, clientCallback<PDUInvokeResp> callback)
        {
            InvokeQueueItem item = new InvokeQueueItem() { request = data, callback = callback };
            lock (lockObject)
            {
                invokeQueue.Add(data.Sequence, item);
            }
            SocketError se = InvokeAsync(data);
            if (se != SocketError.Success)
            {
                Logger.Log.ErrorFormat("SocketException SocketError={0}", se);
            }
        }
        public void InvokeAsync(PDUInvokeByName data, clientCallback<PDUInvokeResp> callback)
        {
            InvokeQueueItem item = new InvokeQueueItem() { request = data, callback = callback };
            lock (lockObject)
            {
                invokeQueue.Add(data.Sequence, item);
            }
            SocketError se = InvokeAsync(data);
            if (se != SocketError.Success)
            {
                Logger.Log.ErrorFormat("SocketException SocketError={0}", se);
            }
        }
        public PDUInvokeResp Invoke(PDUInvoke data)
        {
            InvokeQueueItem item = new InvokeQueueItem() { request = data };
            lock (lockObject)
            {
                invokeQueue.Add(data.Sequence, item);
            }
            try
            {
                SocketError se = InvokeAsync(data);
                if (se != SocketError.Success)
                {
                    Logger.Log.ErrorFormat("SocketException SocketError={0}", se);
                    throw new SocketException((int)se);
                }
                if (!item.waitEvent.WaitOne((int)config.Timeout))
                {
                    Logger.Log.WarnFormat("Таймаут при ожидании результата Sequence={0}", data.Sequence);
                    throw new TimeoutException(String.Format("Таймаут при ожидании результата Sequence={0}", data.Sequence));
                }
                return item.response;
                //TEntity result = item.response.GetInvokeResult<TEntity>();
                //return result;
            }
            finally
            {
                lock (lockObject)
                {
                    invokeQueue.Remove(data.Sequence);
                }
            }
        }
        public TEntity Invoke<TEntity>(PDUInvoke data)
        {
            InvokeQueueItem item = new InvokeQueueItem() { request = data };
            lock (lockObject)
            {
                invokeQueue.Add(data.Sequence, item);
            }
            try
            {
                SocketError se = InvokeAsync(data);
                if (se != SocketError.Success)
                {
                    Logger.Log.ErrorFormat("Ошибка транспорта SocketError={0}", se);
                    throw new Exception(String.Format("Ошибка транспорта SocketError={0}", se));
                }
                if (!item.waitEvent.WaitOne((int)config.Timeout))
                {
                    Logger.Log.ErrorFormat("Таймаут при ожидании результата Sequence={0}", data.Sequence);
                    throw new TimeoutException(String.Format("Таймаут при ожидании результата Sequence={0}", data.Sequence));
                }
                TEntity result = item.response.GetInvokeResult<TEntity>();
                return result;
            }
            finally
            {
                lock (lockObject)
                {
                    invokeQueue.Remove(data.Sequence);
                }
            }
        }
        public PDUInvokeResp Invoke(PDUInvokeByName data)
        {
            InvokeQueueItem item = new InvokeQueueItem() { request = data };
            lock (lockObject)
            {
                invokeQueue.Add(data.Sequence, item);
            }
            try
            {
                SocketError se = InvokeAsync(data);
                if (se != SocketError.Success)
                {
                    Logger.Log.ErrorFormat("Ошибка транспорта SocketError={0}", se);
                    throw new Exception(String.Format("Ошибка транспорта SocketError={0}", se));
                }
                if (!item.waitEvent.WaitOne((int)config.Timeout))
                {
                    Logger.Log.ErrorFormat("Таймаут при ожидании результата Sequence={0}", data.Sequence);
                    throw new TimeoutException(String.Format("Таймаут при ожидании результата Sequence={0}", data.Sequence));
                }
                //TEntity result = item.response.GetInvokeResult<TEntity>();
                //return result;
                return item.response;
            }
            finally
            {
                lock (lockObject)
                {
                    invokeQueue.Remove(data.Sequence);
                }
            }
        }
        public TEntity Invoke<TEntity>(PDUInvokeByName data)
        {
            InvokeQueueItem item = new InvokeQueueItem() { request = data };
            lock (lockObject)
            {
                invokeQueue.Add(data.Sequence, item);
            }
            try
            {
                SocketError se = InvokeAsync(data);
                if (se != SocketError.Success)
                {
                    Logger.Log.ErrorFormat("Ошибка транспорта SocketError={0}", se);
                    throw new Exception(String.Format("Ошибка транспорта SocketError={0}", se));
                }
                if (!item.waitEvent.WaitOne((int)config.Timeout))
                {
                    Logger.Log.ErrorFormat("Таймаут при ожидании результата Sequence={0}", data.Sequence);
                    throw new TimeoutException(String.Format("Таймаут при ожидании результата Sequence={0}", data.Sequence));
                }
                TEntity result = item.response.GetInvokeResult<TEntity>();
                return result;
            }
            finally
            {
                lock (lockObject)
                {
                    invokeQueue.Remove(data.Sequence);
                }
            }
        }

        private void Start()
        {
            SetupServerSocket();
        }
        private void SetupServerSocket()
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.ReceiveTimeout = (int)config.Timeout * 2;
            _socket.SendTimeout = (int)config.Timeout * 2;
            //_socket.Bind(iprp);
        }

        #region ringBuffer
//        private uint GetLenPacket(uint start)
//        {
//            uint result = 0;
//            lock (lockObject)
//            {
//                if (start + 4 <= receivedBuffer.Length)
//                {
//                    Tools.ConvertArrayToUInt(receivedBuffer, (int)start, ref result);
//                }
//                else
//                {
//                    byte[] tmp = new byte[4];
//                    Array.Copy(receivedBuffer, start, tmp, 0, receivedBuffer.Length - start);
//                    Array.Copy(receivedBuffer, 0, tmp, receivedBuffer.Length - start, 4 - (receivedBuffer.Length - start));
//                    Tools.ConvertArrayToUInt(tmp, 0, ref result);
//                }
//            }
//            return result;
//        }
//        private uint ReceivedLength
//        {
//            get
//            {
//                lock (lockObject)
//                {
//                    if (positionFinish > positionStart)
//                    {
//                        return positionFinish - (uint)positionStart;
//                    }
//                    else if (positionFinish < positionStart)
//                    {
//                        return (uint)receivedBuffer.Length - (uint)positionStart + positionFinish;
//                    }
//                    else
//                    {
//                        return (uint)receivedBuffer.Length;
//                    }
//                }
//            }
//        }
//        public void AddReceived(byte[] data)
//        {
//            try
//            {
//                uint dataLength = (uint)data.Length;
//                uint receivedBufferLength = 0;
//#if DEBUG_Performance
//                using (System.Diagnostics.PerformanceCounter performanceCounter = new System.Diagnostics.PerformanceCounter("AverageCounter64SampleCategory", "AverageCounter64Sample"))
//                {
//                    System.Diagnostics.CounterSample cs1;
//                    System.Diagnostics.CounterSample cs2;
//                    cs2 = performanceCounter.NextSample();
//#endif
//                receivedBufferLength = (uint)receivedBuffer.Length;
//#if DEBUG_Performance
//                    cs1 = cs2;
//                    cs2 = performanceCounter.NextSample();
//                    Double milliseconds = (Double)(cs2.CounterTimeStamp - cs1.CounterTimeStamp) / (Double)cs1.CounterFrequency * 1000;
//                }
//#endif
//                lock (lockObject)
//                {

//                    uint rl = ReceivedLength;
//                    //перераспределяем буффер если нужно
//                    if (receivedBufferLength < rl + dataLength)
//                    {
//                        Array.Resize(ref receivedBuffer, (int)receivedBufferLength + (int)dataLength);
//                        if (positionStart > positionFinish)
//                        {
//                            Logger.Log.WarnFormat("Перераспределение размера буфера.");
//                            Array.Copy(receivedBuffer, positionStart, receivedBuffer, positionStart + dataLength, receivedBufferLength - positionStart);
//                            Array.Clear(receivedBuffer, (int)positionStart, (int)dataLength);
//                            positionStart += (int)dataLength;
//                        }
//                        else if (positionStart == positionFinish)
//                        {
//                            if (rl > 0)
//                            {
//                                Logger.Log.WarnFormat("Перераспределение размера буфера. else");
//                                Array.Copy(receivedBuffer, positionStart, receivedBuffer, positionStart + dataLength, receivedBufferLength - positionStart);
//                                Array.Clear(receivedBuffer, (int)positionStart, (int)dataLength);
//                                positionStart += (int)dataLength;
//                            }
//                        }
//                        receivedBufferLength = (uint)receivedBuffer.Length;
//                        Logger.Log.WarnFormat("Перераспределение размера буфера. Новый размер буфера \"{0}\"", receivedBufferLength);

//                    }
//                    //переносим пришедшие данные в наш кольцевой буфер
//                    if (positionFinish > positionStart)
//                    {
//                        if (positionFinish + dataLength <= receivedBufferLength)
//                        {
//                            System.Buffer.BlockCopy(data, 0, receivedBuffer, (int)positionFinish, (int)dataLength);
//                        }
//                        else
//                        {
//                            Array.Copy(data, 0, receivedBuffer, positionFinish, receivedBufferLength - positionFinish);
//                            Array.Copy(data, receivedBufferLength - positionFinish, receivedBuffer, 0, dataLength - (receivedBufferLength - positionFinish));
//                        }
//                        positionFinish += dataLength;
//                        if (positionFinish > receivedBufferLength)
//                        {
//                            positionFinish -= receivedBufferLength;
//                        }
//                    }
//                    else if (positionFinish < positionStart)
//                    {
//                        System.Buffer.BlockCopy(data, 0, receivedBuffer, (int)positionFinish, (int)dataLength);
//                        positionFinish += dataLength;
//                    }
//                    else
//                    {
//                        if (positionFinish + dataLength <= receivedBufferLength)
//                        {
//                            System.Buffer.BlockCopy(data, 0, receivedBuffer, (int)positionFinish, (int)dataLength);
//                        }
//                        else
//                        {
//                            Array.Copy(data, 0, receivedBuffer, positionFinish, receivedBufferLength - positionFinish);
//                            Array.Copy(data, receivedBufferLength - positionFinish, receivedBuffer, 0, dataLength - (receivedBufferLength - positionFinish));
//                        }
//                        positionFinish += dataLength;
//                        if (positionFinish > receivedBufferLength)
//                        {
//                            positionFinish -= receivedBufferLength;
//                        }

//                        //positionFinish = 0;
//                        //positionStart = 0;
//                        //System.Buffer.BlockCopy(data, 0, requestBuffer, 0, (int)dataLength);
//                        //positionFinish += dataLength;
//                    }
//                    //запускаем цикл обработки пакетов находящихся в кольцевом буффере
//                    for (; ; )
//                    {
//                        uint requestLength = ReceivedLength;
//                        if (requestLength >= 16)
//                        {
//                            uint FirstPacketLength = GetLenPacket((uint)positionStart);
//                            if (FirstPacketLength <= requestLength)
//                            {
//                                byte[] packet = new byte[FirstPacketLength];
//                                if (positionStart + FirstPacketLength <= receivedBufferLength)
//                                {
//                                    Array.Copy(receivedBuffer, positionStart, packet, 0, FirstPacketLength);
//                                    Array.Clear(receivedBuffer, (int)positionStart, (int)FirstPacketLength);
//                                    positionStart += (int)FirstPacketLength;
//                                }
//                                else
//                                {
//                                    Array.Copy(receivedBuffer, positionStart, packet, 0, receivedBufferLength - positionStart);
//                                    Array.Copy(receivedBuffer, 0, packet, receivedBufferLength - positionStart, FirstPacketLength - (receivedBufferLength - positionStart));
//                                    Array.Clear(receivedBuffer, (int)positionStart, (int)(receivedBufferLength - positionStart));
//                                    Array.Clear(receivedBuffer, 0, (int)(FirstPacketLength - (receivedBufferLength - positionStart)));
//                                    positionStart = (int)FirstPacketLength - ((int)receivedBufferLength - positionStart);
//                                }
//                                Task doRequestWork = new Task(DoWorkRequest, packet);
//                                doRequestWork.Start();
//                            }
//                            else
//                            {
//                                break;
//                            }
//                        }
//                        else
//                        {
//                            break;
//                        }
//                    }
//                }
//            }
//            catch (Exception exc)
//            {
//                Logger.Log.Error("Ошибка при выполнении AddReceived", exc);
//            }
//        }
        public void AddReceived(byte[] data)
        {
            ringBuffer.Add(data, DoWorkRequest);
        }
        #endregion

        private void DoWorkRequest(object state)
        {
            try
            {
                byte[] packet = state as byte[];
                if (packet == null)
                {
                    Logger.Log.Fatal("Буффер пакета не должен быть null");
                    throw new ArgumentException("Буффер пакета не должен быть null");
                }

                uint Command = 0;
                Tools.ConvertArrayToUInt(packet, 4, ref Command);

                switch (Command)
                {
                    case 0x80000000://generic_nack
                        #region generic_nack
                        try
                        {
                            PDUGenericNack pgn = new PDUGenericNack(packet);

                            if (OnGenericNackCompleeted != null)
                            {
                                try
                                {
                                    OnGenericNackCompleeted(pgn);
                                }
                                catch (Exception exc)
                                {
                                    Logger.Log.Warn("Ошибка при пользовательской обработке OnGenericNackCompleeted ", exc);
                                }
                            }
                        }
                        catch (Exception exc)
                        {
                            Logger.Log.Error("Неизвестная ошибка в case 0x80000000 ReceiveCallback | ", exc);
                        }
                        #endregion
                        break;
                    case 0x80000003:
                    case 0x80000013:
                        #region invoke_response
                        try
                        {
                            PDUInvokeResp pir = new PDUInvokeResp(packet);

                            if (invokeQueue.ContainsKey(pir.Sequence))
                            {
                                InvokeQueueItem item = invokeQueue[pir.Sequence];
                                item.response = pir;
                                item.waitEvent.Set();
                                if (item.callback != null)
                                {
                                    try
                                    {
                                        item.callback(pir);
                                    }
                                    catch (Exception exc)
                                    {
                                        Logger.Log.Warn("Ошибка при пользовательской обработке в методе обратного вызова ", exc);
                                    }
                                }
                            }

                            if (OnInvokeCompleeted != null)
                            {
                                try
                                {
                                    OnInvokeCompleeted(pir);
                                }
                                catch (Exception exc)
                                {
                                    Logger.Log.Warn("Ошибка при пользовательской обработке OnInvokeCompleeted ", exc);
                                }
                            }
                        }
                        catch (Exception exc)
                        {
                            Logger.Log.Error("Неизвестная ошибка в case 0x80000003 ReceiveCallback | ", exc);
                        }
                        #endregion
                        break;
                    case 0x80000009:
                        #region bind_transceiver_response
                        try
                        {
                            PDUBindTransceiverResp pbtr = new PDUBindTransceiverResp(packet);

                            if (OnBindTransceiverCompleeted != null)
                            {
                                try
                                {
                                    OnBindTransceiverCompleeted(pbtr);
                                }
                                catch (Exception exc)
                                {
                                    Logger.Log.Warn("Ошибка при пользовательской обработке OnBindTransceiverCompleeted ", exc);
                                }
                            }
                            state = pbtr.CommandState == 0 ? ClientState.ENABLED : ClientState.DISABLED;
                        }
                        catch (Exception exc)
                        {
                            Logger.Log.Error("Неизвестная ошибка в case 0x80000009 ReceiveCallback | ", exc);
                        }
                        #endregion
                        break;
                    case 0x00000015:
                        #region enquire_link
                        try
                        {
                            PDUEnquireLink pel = new PDUEnquireLink(packet);
                            if (OnEnquireLinkCompleeted != null)
                            {
                                try
                                {
                                    OnEnquireLinkCompleeted(pel);
                                }
                                catch (Exception exc)
                                {
                                    Logger.Log.Warn("Ошибка при пользовательской обработке OnEnquireLinkCompleeted ", exc);
                                }
                            }

                            PDUEnquireLinkResp pelr = new PDUEnquireLinkResp(pel.CommandState, pel.Sequence++);

                            lock (lockObject)
                            {
                                try
                                {
                                    SocketError se = SocketError.Success;
                                    Send(pelr.AllData, 0, pelr.Lenght, SocketFlags.None, out se);
                                }
                                catch (SocketException e)
                                {
                                    Logger.Log.Error("Ошибка в enquire_link send", e);
                                }
                            }
                        }
                        catch (Exception exc)
                        {
                            Logger.Log.Error("Неизвестная ошибка в case 0x80000009 ReceiveCallback | ", exc);
                        }
                        #endregion
                        break;
                    case 0x80000015:
                        #region enquire_link_response
                        try
                        {
                            HeadPDU head = new HeadPDU(packet);
                            PDUEnquireLinkResp pelr = new PDUEnquireLinkResp(head.commandstate, head.sequence);

                            if (OnEnquireLinkRespCompleeted != null)
                            {
                                try
                                {
                                    OnEnquireLinkRespCompleeted(pelr);
                                }
                                catch (Exception exc)
                                {
                                    Logger.Log.Warn("Ошибка при пользовательской обработке OnEnquireLinkRespCompleeted ", exc);
                                }
                            }
                        }
                        catch (Exception exc)
                        {
                            Logger.Log.Error("Неизвестная ошибка в case 0x80000009 ReceiveCallback | ", exc);
                        }
                        #endregion
                        break;
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Error("Ошибка при выполнении DoWorkRequest", ex);
            }
        }
        private void ReceiveCallback(IAsyncResult result)
        {
            if (_socket == null) return;

            lock (lockObject)
            {
                if (_socket == null) return;
                try
                {
                    int ressived = _socket.EndReceive(result);
                    byte[] packet = new byte[ressived];
                    Array.Copy(buffer, 0, packet, 0, ressived);
                    AddReceived(packet);
                }
                catch (Exception exc)
                {
                    Logger.Log.Error("Ошибка в ReceiveCallback", exc);
                }
                finally
                {
                    try
                    {
                        _socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), this);
                    }
                    catch (SocketException e)
                    {
                        Logger.Log.Error("Ошибка в ReceiveCallback", e);
                        Disonnect();
                    }
                    catch (Exception e)
                    {
                        Logger.Log.Error("Ошибка в ReceiveCallback", e);
                        throw;
                    }
                }
            }
        }

        #region events
        public delegate void clientCallback<TEntity>(TEntity response);
        public event clientCallback<PDUGenericNack> OnGenericNackCompleeted;
        public event clientCallback<PDUEnquireLink> OnEnquireLinkCompleeted;
        public event clientCallback<PDUEnquireLinkResp> OnEnquireLinkRespCompleeted;
        public event clientCallback<PDUBindTransceiverResp> OnBindTransceiverCompleeted;
        public event clientCallback<PDUInvokeResp> OnInvokeCompleeted;
        #endregion
    }
}
