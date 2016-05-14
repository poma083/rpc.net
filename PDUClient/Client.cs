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
using System.Security.Cryptography.X509Certificates;

namespace PDUClient
{
    public enum ClientState { NONE = 0, ENABLED = 1, DISABLED }
    public class Client
    {
        private class WaitEventItem : IDisposable
        {
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
        private class InvokeQueueItem<Resp> : WaitEventItem
            where Resp : PDU
        {
            public PDU request;
            public Resp response;
            public clientCallback<Resp> callback;
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

        private Dictionary<uint, WaitEventItem> invokeQueue = new Dictionary<uint, WaitEventItem>();

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
            List<WaitEventItem> tmp = new List<WaitEventItem>();
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
            foreach (WaitEventItem item in tmp)
            {
                item.Dispose();
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
        public PDUInvokeSecureByName CreateInvokeSecureByName(String invokeName, object[] arguments,
            StoreName clientCertStoreName, StoreLocation clientCertStoreLocation, string clientCertThumbprint,
            X509Certificate2 serverPublicCertificate)
        {
            PDUInvokeSecureByName pi = null;
            lock (lockObject)
            {
                unchecked
                {
                    sequence++;
                }
                pi = new PDUInvokeSecureByName(0, sequence, invokeName, arguments,
                    clientCertStoreName, clientCertStoreLocation, clientCertThumbprint, serverPublicCertificate);
            }
            return pi;
        }

        private SocketError InvokeAsync(PDU data)
        {
            SocketError se = new SocketError();
            int recv = Send(data.AllData, 0, data.Lenght, SocketFlags.None, out se);
            return se;
        }
        public void InvokeAsync(PDU data, clientCallback<PDUResp> callback)
        {
            InvokeQueueItem<PDUResp> item = new InvokeQueueItem<PDUResp>() { request = data, callback = callback };
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
        public PDU Invoke(PDU data)
        {
            InvokeQueueItem<PDUResp> item = new InvokeQueueItem<PDUResp>() { request = data };
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
            }
            finally
            {
                lock (lockObject)
                {
                    invokeQueue.Remove(data.Sequence);
                }
            }
        }
        public TEntity Invoke<TEntity>(PDU data)
        {
            InvokeQueueItem<PDUResp> item = new InvokeQueueItem<PDUResp>() { request = data };
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

        public void WaitInvoke(string invokeName, clientCallback<PDUWaitResp> callback)
        {
            PDUWait pw = null;
            lock (lockObject)
            {
                unchecked
                {
                    sequence++;
                }
                pw = new PDUWait(0, sequence, invokeName, WaitType.WaitAll);
            }
            InvokeQueueItem<PDUWaitResp> item = new InvokeQueueItem<PDUWaitResp>() { request = pw, callback = callback };
            lock (lockObject)
            {
                invokeQueue.Add(pw.Sequence, item);
            }
            SocketError se = InvokeAsync(pw);
            if (se != SocketError.Success)
            {
                Logger.Log.ErrorFormat("SocketException SocketError={0}", se);
                throw new SocketException((int)se);
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
                MessageType cmd = (MessageType)Command;
                switch (cmd)
                {
                    case MessageType.GenericNack://generic_nack
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
                            Logger.Log.ErrorFormat("Неизвестная ошибка в case {0} ReceiveCallback | {1}", cmd.ToString("g"), exc);
                        }
                        #endregion
                        break;
                    case MessageType.InvokeResp:
                        #region invoke_response
                        try
                        {
                            PDUInvokeResp pir = new PDUInvokeResp(packet);

                            InvokeQueueItem<PDUResp> item = null;
                            lock (lockObject)
                            {
                                if (invokeQueue.ContainsKey(pir.Sequence))
                                {
                                    item = invokeQueue[pir.Sequence] as InvokeQueueItem<PDUResp>;
                                    invokeQueue.Remove(pir.Sequence);
                                }
                            }                            
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
                            Logger.Log.ErrorFormat("Неизвестная ошибка в case {0} ReceiveCallback | {1}", cmd.ToString("g"), exc);
                        }
                        #endregion
                        break;
                    case MessageType.InvokeSecureByNameResp:
                        #region invokeSecure_response
                        try
                        {
                            PDUInvokeSecureResp pir = new PDUInvokeSecureResp(packet,
                                                                            config.ClientCertificate.StoreName,
                                                                            config.ClientCertificate.StoreLocation,
                                                                            config.ClientCertificate.Thumbprint
                                                                        );
                            if (invokeQueue.ContainsKey(pir.Sequence))
                            {
                                InvokeQueueItem<PDUResp> item = invokeQueue[pir.Sequence] as InvokeQueueItem<PDUResp>;
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
                                        Logger.Log.Fatal("Ошибка при пользовательской обработке в методе обратного вызова ", exc);
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
                                    Logger.Log.Fatal("Ошибка при пользовательской обработке OnInvokeCompleeted ", exc);
                                }
                            }
                        }
                        catch (Exception exc)
                        {
                            Logger.Log.FatalFormat("Неизвестная ошибка в case {0} ReceiveCallback | {1}", cmd, exc);
                        }
                        #endregion
                        break;
                    case MessageType.WaitResp:
                        #region wait_response
                        try
                        {
                            PDUWaitResp pwr = new PDUWaitResp(packet);
                            InvokeQueueItem<PDUWaitResp> item = null;
                            lock (lockObject)
                            {
                                if (invokeQueue.ContainsKey(pwr.Sequence))
                                {
                                    item = invokeQueue[pwr.Sequence] as InvokeQueueItem<PDUWaitResp>;
                                    invokeQueue.Remove(pwr.Sequence);
                                }
                            }
                            item.response = pwr;
                            if (item.callback != null)
                            {
                                try
                                {
                                    item.callback(pwr);
                                }
                                catch (Exception exc)
                                {
                                    Logger.Log.Warn("Ошибка при пользовательской обработке в методе обратного вызова ", exc);
                                }
                            }
                        }
                        catch (Exception exc)
                        {
                            Logger.Log.ErrorFormat("Неизвестная ошибка в case {0} ReceiveCallback | {1}", cmd.ToString("g"), exc);
                        }
                        #endregion
                        break;
                    case MessageType.BindTransceiverResp:
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
                            Logger.Log.ErrorFormat("Неизвестная ошибка в case {0} ReceiveCallback | {1}", cmd.ToString("g"), exc);
                        }
                        #endregion
                        break;
                    case MessageType.EnquireLink:
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
                            Logger.Log.ErrorFormat("Неизвестная ошибка в case {0} ReceiveCallback | {1}", cmd.ToString("g"), exc);
                        }
                        #endregion
                        break;
                    case MessageType.EnquireLinkResp:
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
                            Logger.Log.ErrorFormat("Неизвестная ошибка в case {0} ReceiveCallback | {1}", cmd.ToString("g"), exc);
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
        public event clientCallback<PDUResp> OnInvokeCompleeted;
        #endregion
    }
}
