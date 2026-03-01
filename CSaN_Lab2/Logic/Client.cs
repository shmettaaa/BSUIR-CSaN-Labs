using System;

namespace CSaN_Lab2.Logic
{
    public class Client
    {
        private IClientConnection _connection;

        public Client()
        {
            _connection = new TcpConnection();
        }

        public void Connect(string ip, int port)
        {
            _connection.Connect(ip, port);
        }

        public void Send(byte[] data)
        {
            _connection.Send(data);
        }

        public byte[] Receive()
        {
            return _connection.Receive();
        }

        public void Disconnect()
        {
            _connection.Disconnect();
        }
    }
}