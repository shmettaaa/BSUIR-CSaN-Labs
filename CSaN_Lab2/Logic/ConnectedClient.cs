using System.Net.Sockets;

namespace CSaN_Lab2.Logic
{
    public class ConnectedClient
    {
        public TcpClient TcpClient { get; }
        public string IP { get; }
        public NetworkStream Stream { get; }

        public ConnectedClient(TcpClient tcpClient)
        {
            TcpClient = tcpClient;
            IP = ((System.Net.IPEndPoint)tcpClient.Client.RemoteEndPoint!).Address.ToString();
            Stream = tcpClient.GetStream();
        }

        public void Close()
        {
            Stream?.Close();
            TcpClient?.Close();
        }
    }
}