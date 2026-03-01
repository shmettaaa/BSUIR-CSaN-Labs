using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace CSaN_Lab2.Logic
{
    public class TcpConnection : IClientConnection
    {
        private Socket _socket;
        private bool _isServer;

        public TcpConnection(bool isServer = false)
        {
            _isServer = isServer;
            _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        }

        public void Connect(string ip, int port)
        {
            try
            {
                if (_isServer)
                {
                    _socket.Bind(new IPEndPoint(IPAddress.Any, port));
                    _socket.Listen(10); 
                }
                else
                {
                    _socket.Connect(new IPEndPoint(IPAddress.Parse(ip), port));
                }
            }
            catch (SocketException ex)
            {
                throw new SocketConnectionException("Ошибка подключения: " + ex.Message, ex);
            }
        }

        public void Send(byte[] data)
        {
            try
            {
                _socket.Send(data);
            }
            catch (SocketException ex)
            {
                throw new SocketConnectionException("Ошибка отправки: " + ex.Message, ex);
            }
        }

        public byte[] Receive()
        {
            try
            {
                byte[] buffer = new byte[1024];
                int received = _socket.Receive(buffer);
                byte[] result = new byte[received];
                Array.Copy(buffer, result, received);
                return result;
            }
            catch (SocketException ex)
            {
                throw new SocketConnectionException("Ошибка получения: " + ex.Message, ex);
            }
        }

        public void Disconnect()
        {
            _socket?.Close();
            _socket?.Dispose();
        }

        public void Dispose()
        {
            Disconnect();
        }
    }

    public class SocketConnectionException : Exception
    {
        public SocketConnectionException(string message, Exception inner) : base(message, inner) { }
    }
}
