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
    public class Client
    {
        private class InvokeQueueItem : IDisposable
        {
            public PDU request;
            public PDUInvokeResp response;
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

        private byte[] receivedBuffer = new byte[1024 * 1024];
        private uint positionStart = 0;
        private uint positionFinish = 0;

        private Timer generic_nack_timer;

        private Dictionary<uint, InvokeQueueItem> invokeQueue = new Dictionary<uint, InvokeQueueItem>();
        #endregion

        private void sendGenericNack(Object state)
        {
            PDUGenericNack pgn = null;
            lock (lockObject)
            {
                unchecked
                {
                    sequence++;
                }
                pgn = new PDUGenericNack(0, sequence);
            }
            SocketError se = new SocketError();
            int recv = _socket.Send(pgn.AllData, 0, pgn.Lenght, SocketFlags.None, out se);
        }

        public Client(ClientCfgClass cfg)//string _host, ushort _port, uint _timeout, uint generikNack)
        {
            config = cfg;
            IPAddress adr = IPAddress.Any;
            IPAddress.TryParse(config.Host, out adr);
            IPEndPoint myEndpoint = new IPEndPoint(adr, config.Port);
            Start(myEndpoint);
        }
        public PDUBindTransceiver Connect(string login, string password)
        {
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
                    pbt = new PDUBindTransceiver(0, sequence, login, password, config.Timeout);
                }
                string sss = pbt.SystemID;
                int recv = _socket.Send(pbt.AllData, 0, pbt.Lenght, SocketFlags.None, out se);

                lock (lockObject)
                {
                    if (_socket != null)
                    {
                        _socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), this);
                    }
                }
                System.Threading.Thread.Sleep(100);
                return pbt;
            }
            return null;
        }
        public void Disonnect()
        {
            if (_socket.Connected)
            {
                _socket.Disconnect(false);
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
        
        public SocketError InvokeAsync(PDUInvoke data)
        {
            SocketError se = new SocketError();
            int recv = _socket.Send(data.AllData, 0, data.Lenght, SocketFlags.None, out se);
            return se;
        }
        public SocketError InvokeAsync(PDUInvokeByName data)
        {
            SocketError se = new SocketError();
            int recv = _socket.Send(data.AllData, 0, data.Lenght, SocketFlags.None, out se);
            return se;
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
                    throw new Exception(String.Format("Ошибка транспорта SocketError={0}", se));
                }
                if(!item.waitEvent.WaitOne((int)config.Timeout))
                {
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
                    throw new Exception(String.Format("Ошибка транспорта SocketError={0}", se));
                }
                if (!item.waitEvent.WaitOne((int)config.Timeout))
                {
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

        private void Start(IPEndPoint ipep)
        {
            SetupServerSocket(ipep);
        }
        private void SetupServerSocket(IPEndPoint iprp)
        {
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _socket.ReceiveTimeout = (int)config.Timeout * 2;
            _socket.SendTimeout = (int)config.Timeout * 2;
            //_socket.Bind(iprp);
        }

        private uint ReceivedBufferLength
        {
            get
            {
                lock (lockObject)
                {
                    if (positionFinish >= positionStart)
                    {
                        return positionFinish - positionStart;
                    }
                    else
                    {
                        return (uint)receivedBuffer.Length - positionStart + positionFinish;
                    }
                }
            }
        }
        private void AddReceived(byte[] data)
        {
            try
            {
                lock (lockObject)
                {
                    if (receivedBuffer.Length - ReceivedBufferLength < (uint)data.Length)
                    {
                        throw new StackOverflowException("Произошло переполнение буффера requestBuffer");
                    }
                    uint old_positionFinish = positionFinish;
                    uint new_positionFinish = positionFinish + (uint)data.Length;
                    if (new_positionFinish > receivedBuffer.Length)
                    {
                        new_positionFinish = new_positionFinish - (uint)receivedBuffer.Length;
                    }
                    if (data.Length <= receivedBuffer.Length - old_positionFinish)
                    {
                        Array.Copy(data, 0, receivedBuffer, old_positionFinish, data.Length);
                    }
                    else
                    {
                        Array.Copy(data, 0, receivedBuffer, old_positionFinish, receivedBuffer.Length - old_positionFinish);
                        Array.Copy(data, receivedBuffer.Length - old_positionFinish, receivedBuffer, 0, data.Length - (receivedBuffer.Length - old_positionFinish));
                    }
                    positionFinish = new_positionFinish;
                }

                for (; ; )
                {
                    lock (lockObject)
                    {
                        uint requestLength = ReceivedBufferLength;
                        if (requestLength >= 16)
                        {
                            uint FirstPacketLength = 0;
                            if (positionStart + 4 <= receivedBuffer.Length)
                            {
                                Tools.ConvertArrayToUInt(receivedBuffer, (int)positionStart, ref FirstPacketLength);
                            }
                            else
                            {
                                byte[] tmp = new byte[4];
                                Array.Copy(receivedBuffer, positionStart, tmp, 0, receivedBuffer.Length - positionStart);
                                Array.Copy(receivedBuffer, 0, tmp, receivedBuffer.Length - positionStart, 4 - (receivedBuffer.Length - positionStart));
                                Tools.ConvertArrayToUInt(tmp, 0, ref FirstPacketLength);
                            }
                            if (FirstPacketLength <= requestLength)
                            {
                                byte[] packet = new byte[FirstPacketLength];
                                if (positionStart + FirstPacketLength < receivedBuffer.Length)
                                {
                                    Array.Copy(receivedBuffer, positionStart, packet, 0, FirstPacketLength);
                                    positionStart += FirstPacketLength;
                                }
                                else
                                {
                                    Array.Copy(receivedBuffer, positionStart, packet, 0, receivedBuffer.Length - positionStart);
                                    Array.Copy(receivedBuffer, 0, packet, receivedBuffer.Length - positionStart, FirstPacketLength - (receivedBuffer.Length - positionStart));
                                    positionStart = FirstPacketLength - ((uint)receivedBuffer.Length - positionStart);
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
            catch (Exception ex)
            {
                Logger.Log.Error("Ошибка при выполнении AddReceived", ex);
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
                                    Logger.Log.Fatal("Ошибка при пользовательской обработке OnGenericNackCompleeted ", exc);
                                }
                            }
                        }
                        catch (Exception exc)
                        {
                            Logger.Log.Fatal("Неизвестная ошибка в case 0x80000000 ReceiveCallback | ", exc);
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
                            Logger.Log.Fatal("Неизвестная ошибка в case 0x80000003 ReceiveCallback | ", exc);
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
                                    Logger.Log.Fatal("Ошибка при пользовательской обработке OnBindTransceiverCompleeted ", exc);
                                }
                            }

                            TimerCallback timerDelegate = new TimerCallback(sendGenericNack);
                            generic_nack_timer = new Timer(timerDelegate, null, config.GenericNackPeriod, config.GenericNackPeriod);
                        }
                        catch (Exception exc)
                        {
                            Logger.Log.Fatal("Неизвестная ошибка в case 0x80000009 ReceiveCallback | ", exc);
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
                                    Logger.Log.Fatal("Ошибка при пользовательской обработке OnEnquireLinkCompleeted ", exc);
                                }
                            }

                            PDUEnquireLinkResp pelr = new PDUEnquireLinkResp(pel.CommandState, pel.Sequence++);

                            lock (lockObject)
                            {
                                SocketError se = SocketError.Success;
                                _socket.Send(pelr.AllData, 0, pelr.Lenght, SocketFlags.None, out se);
                            }
                        }
                        catch (Exception exc)
                        {
                            Logger.Log.Fatal("Неизвестная ошибка в case 0x80000009 ReceiveCallback | ", exc);
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
                                    Logger.Log.Fatal("Ошибка при пользовательской обработке OnEnquireLinkRespCompleeted ", exc);
                                }
                            }
                        }
                        catch (Exception exc)
                        {
                            Logger.Log.Fatal("Неизвестная ошибка в case 0x80000009 ReceiveCallback | ", exc);
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
                lock (lockObject)
                {
                    if (_socket != null)
                    {
                        try
                        {
                            _socket.BeginReceive(buffer, 0, buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), this);
                        }
                        catch
                        {
                            Console.Read();
                        }
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
