using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSaN_Lab2.Logic
{
    public class Server
    {
        private TcpListener? _listener;
        private CancellationTokenSource? _cts;
        private bool _isRunning;

        private readonly List<ConnectedClient> _connectedClients = new();

        public event Action<string>? LogReceived;
        public event Action<string>? ClientConnected;
        public event Action<string>? ClientDisconnected;

        public async Task StartAsync(string ipString, int port)
        {
            if (_isRunning) return;

            try
            {
                IPAddress address = string.IsNullOrWhiteSpace(ipString) || ipString.Trim() == "0.0.0.0"
                    ? IPAddress.Any
                    : IPAddress.Parse(ipString);

                _listener = new TcpListener(address, port);
                _listener.Start();

                _isRunning = true;
                _cts = new CancellationTokenSource();

                LogReceived?.Invoke($"Сервер запущен на {address}:{port}");

                await AcceptClientsLoopAsync(_cts.Token);
            }
            catch (Exception ex)
            {
                LogReceived?.Invoke($"Ошибка запуска: {ex.Message}");
            }
        }

        private async Task AcceptClientsLoopAsync(CancellationToken token)
        {
            try
            {
                while (_isRunning && !token.IsCancellationRequested)
                {
                    TcpClient rawClient = await _listener!.AcceptTcpClientAsync();
                    var client = new ConnectedClient(rawClient);

                    lock (_connectedClients)
                        _connectedClients.Add(client);

                    ClientConnected?.Invoke(client.IP);
                    LogReceived?.Invoke($"Подключился клиент: {client.IP}");

                    _ = HandleClientAsync(client, token);
                }
            }
            catch (Exception ex)
            {
                LogReceived?.Invoke($"Ошибка Accept: {ex.Message}");
            }
        }

        private async Task HandleClientAsync(ConnectedClient client, CancellationToken token)
        {
            string ip = client.IP;

            try
            {
                await using NetworkStream stream = client.Stream;
                byte[] typeBuf = new byte[1];

                while (_isRunning && !token.IsCancellationRequested && client.TcpClient.Connected)
                {
                    int read = await stream.ReadAsync(typeBuf, 0, 1, token);
                    if (read == 0) break;

                    byte type = typeBuf[0];

                    if (type == 1)
                    {
                        string text = await ReadStringExactAsync(stream, token);
                        LogReceived?.Invoke($"[{ip}]: {text}");
                    }
                    else if (type == 2)
                    {
                        string savedPath = await FileTransfer.ReceiveFileAsync(stream, token);
                        LogReceived?.Invoke($"[{ip}] отправил файл: {Path.GetFileName(savedPath)} (сохранён)");
                    }
                }
            }
            catch (Exception ex)
            {
                LogReceived?.Invoke($"Ошибка клиента {ip}: {ex.Message}");
            }
            finally
            {
                client.Close();

                lock (_connectedClients)
                    _connectedClients.Remove(client);

                ClientDisconnected?.Invoke(ip);
                LogReceived?.Invoke($"Клиент отключился: {ip}");
            }
        }

        public async Task SendToClientAsync(string targetIp, string message)
        {
            byte[] textBytes = Encoding.UTF8.GetBytes(message);
            byte[] lenBytes = BitConverter.GetBytes(textBytes.Length);

            ConnectedClient? target = null;

            lock (_connectedClients)
            {
                target = _connectedClients.Find(c => c.IP == targetIp);
                if (target == null)
                {
                    LogReceived?.Invoke($"Клиент {targetIp} не найден");
                    return;
                }
            }

            try
            {
                await target.Stream.WriteAsync(new byte[] { 1 }, 0, 1);
                await target.Stream.WriteAsync(lenBytes, 0, 4);
                await target.Stream.WriteAsync(textBytes, 0, textBytes.Length);
                await target.Stream.FlushAsync();

                LogReceived?.Invoke($"[Сервер → {targetIp}]: {message}");
            }
            catch (Exception ex)
            {
                LogReceived?.Invoke($"Ошибка отправки {targetIp}: {ex.Message}");
                target.Close();

                lock (_connectedClients)
                    _connectedClients.Remove(target);

                ClientDisconnected?.Invoke(targetIp);
            }
        }

        public async Task SendFileToClientAsync(string targetIp, string filePath)
        {
            ConnectedClient? target = null;

            lock (_connectedClients)
            {
                target = _connectedClients.Find(c => c.IP == targetIp);
                if (target == null)
                {
                    LogReceived?.Invoke($"Клиент {targetIp} не найден");
                    return;
                }
            }

            try
            {
                await FileTransfer.SendFileAsync(target.Stream, filePath);
                LogReceived?.Invoke($"[Сервер → {targetIp}] отправил файл: {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                LogReceived?.Invoke($"Ошибка отправки файла {targetIp}: {ex.Message}");
                target.Close();

                lock (_connectedClients)
                    _connectedClients.Remove(target);

                ClientDisconnected?.Invoke(targetIp);
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _cts?.Cancel();
            _listener?.Stop();

            lock (_connectedClients)
            {
                foreach (var c in _connectedClients.ToArray())
                    c.Close();
                _connectedClients.Clear();
            }

            LogReceived?.Invoke("Сервер остановлен");
        }

        private static async Task<string> ReadStringExactAsync(NetworkStream stream, CancellationToken ct)
        {
            byte[] lenBuf = new byte[4];
            await ReadExactAsync(stream, lenBuf, ct);
            int len = BitConverter.ToInt32(lenBuf);

            byte[] strBuf = new byte[len];
            await ReadExactAsync(stream, strBuf, ct);
            return Encoding.UTF8.GetString(strBuf);
        }

        private static async Task ReadExactAsync(NetworkStream stream, byte[] buffer, CancellationToken ct)
        {
            int total = 0;
            while (total < buffer.Length)
            {
                int read = await stream.ReadAsync(buffer, total, buffer.Length - total, ct);
                if (read == 0) throw new IOException("Соединение разорвано");
                total += read;
            }
        }
    }
}