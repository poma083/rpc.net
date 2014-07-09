using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using PDUDatas;

namespace PDUServer
{
    public class Server
    {
        Socket _serverSocket;
        private SortedList<Guid, ConnectionInfo> _connections = new SortedList<Guid, ConnectionInfo>();
        private ServerCfgClass serverConfig;

        public Server(ServerCfgClass rsc)
        {
            Logger.Log.Debug("Запускаем TCP-сервер...");
            serverConfig = rsc;
        }
        public bool Start()
        {
            try
            {
                SetupServerSocket();
                for (int i = 0; i < 10; i++)
                {
                    _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), _serverSocket);
                }
            }
            catch (Exception ex)
            {
                Logger.Log.Debug("ошибка в функции Start класса SpeechServerListenerSocket", ex);
                return false;
            }
            Logger.Log.Debug("TCP-сервер запущен...");
            return true;
        }
        private void SetupServerSocket()
        {
            // Получаем информацию о локальном компьютере
            IPAddress adr = IPAddress.Any;
            IPAddress.TryParse(serverConfig.Host, out adr);
            IPEndPoint myEndpoint = new IPEndPoint(adr, serverConfig.Port);

            // Создаем сокет, привязываем его к адресу
            // и начинаем прослушивание
            _serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            _serverSocket.Bind(myEndpoint);
            _serverSocket.Listen((int)SocketOptionName.MaxConnections);
        }

        private void AcceptCallback(IAsyncResult result)
        {
            ConnectionInfo connection = null;
            try
            {
                // Завершение операции Accept
                Socket s = (Socket)result.AsyncState;
                connection = new ConnectionInfo(
                        _connections,
                        s.EndAccept(result),
                        serverConfig.EnquireLinkPeriod
                    );
                connection.evGenericNack += _evGenericNack;
                connection.evGenericNackCompleted += _evGenericNackCompleted;
                connection.evConnect += _evConnect;
                connection.evConnectCompleted += _evConnectCompleted;
                connection.evEnquireLink += _evEnquireLink;
                connection.evEnquireLinkCompleted += _evEnquireLinkCompleted;
                connection.evInvoke += _evInvoke;
                connection.evInvokeCompleted += _evInvokeCompleted;

                lock (_connections)
                {
                    try
                    {
                        _connections.Add(connection.Id, connection);
                    }
                    catch (Exception exc)
                    {
                        Logger.Log.Debug("Ошибка при добавлении соединения в список _connections", exc);
                    }
                }

                // Начало операции Receive и новой операции Accept
                connection.BeginReceive(0, new AsyncCallback(ReceiveCallback));
                _serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), result.AsyncState);
            }
            catch (SocketException exc)
            {
                lock (_connections)
                {
                    connection.CloseConnection();
                }
                Logger.Log.Fatal("Socket exception: ", exc);
            }
            catch (Exception exc)
            {
                lock (_connections)
                {
                    connection.CloseConnection();
                }
                Logger.Log.Fatal("Exception: ", exc);
            }
        }
        private void ReceiveCallback(IAsyncResult result)
        {
            ConnectionInfo connection = (ConnectionInfo)result.AsyncState;

            try
            {
                int bytesRead = connection.EndReceive(result);
                if (0 != bytesRead)
                {
                    //if (bytesRead < 16)
                    //{
                    //    throw new IndexOutOfRangeException("Клличество принятых байт меньше длинны заголовка");
                    //}
                    if (bytesRead > 0)
                    {
                        byte[] packet = null;
                        packet = new byte[bytesRead];
                        Array.Copy(connection.Buffer, 0, packet, 0, bytesRead);
                        connection.AddRequest(packet);
                    }
                    return;
                }
                else
                {
                    Logger.Log.Info("От удалёного клиента c address=\"" + connection.RemoteEndPoint.ToString() + "\" получен пакет размером 0 байт - разрываем соединение");
                    lock (_connections)
                    {
                        _connections.Remove(connection.Id);
                        connection.CloseConnection();
                    }
                }
            }
            catch (SocketException exc)
            {
                if (exc.ErrorCode == Convert.ToInt32(SocketError.ConnectionReset))
                {
                    Logger.Log.Warn("Удалёный клиент c address=\"" + connection.RemoteEndPoint.ToString() + "\" разорвал соединение");
                    //lock (_connections)
                    //{
                    //    _connections.Remove(connection.Id);
                    //}
                }
                lock (_connections)
                {
                    connection.CloseConnection();
                }
            }
            catch (Exception exc)
            {
                lock (_connections)
                {
                    connection.CloseConnection();
                }
                Logger.Log.Fatal("Неизвестная ошибка в ReceiveCallback", exc);
            }
            finally
            {
                connection.BeginReceive(0, new AsyncCallback(ReceiveCallback));
            }
        }

        #region events
        //generic_nack
        private event BeforeEventHandler _evGenericNack;
        public event BeforeEventHandler evGenericNack
        {
            add
            {
                _evGenericNack += value;
                lock (_connections)
                {
                    foreach (ConnectionInfo ci in _connections.Values)
                    {
                        ci.evGenericNack += value;
                    }
                }
            }
            remove
            {
                _evGenericNack -= value;
                lock (_connections)
                {
                    foreach (ConnectionInfo ci in _connections.Values)
                    {
                        ci.evGenericNack -= value;
                    }
                }
            }
        }
        private event AffterEventHandler _evGenericNackCompleted;
        public event AffterEventHandler evGenericNackCompleted
        {
            add
            {
                _evGenericNackCompleted += value;
                lock (_connections)
                {
                    foreach (ConnectionInfo ci in _connections.Values)
                    {
                        ci.evGenericNackCompleted += value;
                    }
                }
            }
            remove
            {
                _evGenericNackCompleted -= value;
                lock (_connections)
                {
                    foreach (ConnectionInfo ci in _connections.Values)
                    {
                        ci.evGenericNackCompleted -= value;
                    }
                }
            }
        }
        //bind_transceiver
        private event BeforeEventHandler _evConnect;
        public event BeforeEventHandler evConnect
        {
            add
            {
                _evConnect += value;
                lock (_connections)
                {
                    foreach (ConnectionInfo ci in _connections.Values)
                    {
                        ci.evConnect += value;
                    }
                }
            }
            remove
            {
                _evConnect -= value;
                lock (_connections)
                {
                    foreach (ConnectionInfo ci in _connections.Values)
                    {
                        ci.evConnect -= value;
                    }
                }
            }
        }
        private event AffterEventHandler _evConnectCompleted;
        public event AffterEventHandler evConnectCompleted
        {
            add
            {
                _evConnectCompleted += value;
                lock (_connections)
                {
                    foreach (ConnectionInfo ci in _connections.Values)
                    {
                        ci.evConnectCompleted += value;
                    }
                }
            }
            remove
            {
                _evConnectCompleted -= value;
                lock (_connections)
                {
                    foreach (ConnectionInfo ci in _connections.Values)
                    {
                        ci.evConnectCompleted -= value;
                    }
                }
            }
        }
        //enquire_link
        private event BeforeEventHandler _evEnquireLink;
        public event BeforeEventHandler evEnquireLink
        {
            add
            {
                _evEnquireLink += value;
                lock (_connections)
                {
                    foreach (ConnectionInfo ci in _connections.Values)
                    {
                        ci.evEnquireLink += value;
                    }
                }
            }
            remove
            {
                _evEnquireLink -= value;
                lock (_connections)
                {
                    foreach (ConnectionInfo ci in _connections.Values)
                    {
                        ci.evEnquireLink -= value;
                    }
                }
            }
        }
        private event AffterEventHandler _evEnquireLinkCompleted;
        public event AffterEventHandler evEnquireLinkCompleted
        {
            add
            {
                _evEnquireLinkCompleted += value;
                lock (_connections)
                {
                    foreach (ConnectionInfo ci in _connections.Values)
                    {
                        ci.evEnquireLinkCompleted += value;
                    }
                }
            }
            remove
            {
                _evEnquireLinkCompleted -= value;
                lock (_connections)
                {
                    foreach (ConnectionInfo ci in _connections.Values)
                    {
                        ci.evEnquireLinkCompleted -= value;
                    }
                }
            }
        }
        //invoke
        private event BeforeEventHandler _evInvoke;
        public event BeforeEventHandler evInvoke
        {
            add
            {
                _evInvoke += value;
                lock (_connections)
                {
                    foreach (ConnectionInfo ci in _connections.Values)
                    {
                        ci.evInvoke += value;
                    }
                }
            }
            remove
            {
                _evInvoke -= value;
                lock (_connections)
                {
                    foreach (ConnectionInfo ci in _connections.Values)
                    {
                        ci.evInvoke -= value;
                    }
                }
            }
        }
        private event AffterEventHandler _evInvokeCompleted;
        public event AffterEventHandler evInvokeCompleted
        {
            add
            {
                _evInvokeCompleted += value;
                lock (_connections)
                {
                    foreach (ConnectionInfo ci in _connections.Values)
                    {
                        ci.evInvokeCompleted += value;
                    }
                }
            }
            remove
            {
                _evInvokeCompleted -= value;
                lock (_connections)
                {
                    foreach (ConnectionInfo ci in _connections.Values)
                    {
                        ci.evInvokeCompleted -= value;
                    }
                }
            }
        }
        //invokeByName
        private event BeforeEventHandler _evInvokeByName;
        public event BeforeEventHandler evInvokeByName
        {
            add
            {
                _evInvokeByName += value;
                lock (_connections)
                {
                    foreach (ConnectionInfo ci in _connections.Values)
                    {
                        ci.evInvokeByName += value;
                    }
                }
            }
            remove
            {
                _evInvokeByName -= value;
                lock (_connections)
                {
                    foreach (ConnectionInfo ci in _connections.Values)
                    {
                        ci.evInvokeByName -= value;
                    }
                }
            }
        }
        private event AffterEventHandler _evInvokeByNameCompleted;
        public event AffterEventHandler evInvokeByNameCompleted
        {
            add
            {
                _evInvokeByNameCompleted += value;
                lock (_connections)
                {
                    foreach (ConnectionInfo ci in _connections.Values)
                    {
                        ci.evInvokeByNameCompleted += value;
                    }
                }
            }
            remove
            {
                _evInvokeByNameCompleted -= value;
                lock (_connections)
                {
                    foreach (ConnectionInfo ci in _connections.Values)
                    {
                        ci.evInvokeByNameCompleted -= value;
                    }
                }
            }
        }
        #endregion
    }
}
