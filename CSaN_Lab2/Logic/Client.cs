using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CSaN_Lab2.Logic
{
    public class Client
    {
        private TcpClient? _client;
        private NetworkStream? _stream;
        private CancellationTokenSource? _cts;
        private bool _isConnected;

        public event Action<string>? LogReceived;
        public event Action? Connected;
        public event Action? Disconnected;

        public async Task ConnectAsync(string ip, int port)
        {
            if (_isConnected) return;

            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(ip, port);

                _stream = _client.GetStream();
                _isConnected = true;
                _cts = new CancellationTokenSource();

                LogReceived?.Invoke($"Подключено к {ip}:{port}");
                Connected?.Invoke();

                _ = ReceiveLoopAsync(_cts.Token);
            }
            catch (Exception ex)
            {
                LogReceived?.Invoke($"Ошибка подключения: {ex.Message}");
            }
        }

        public async Task SendTextAsync(string text)
        {
            if (!_isConnected || _stream == null) return;

            try
            {
                byte[] textBytes = Encoding.UTF8.GetBytes(text);
                byte[] lenBytes = BitConverter.GetBytes(textBytes.Length);

                await _stream.WriteAsync(new byte[] { 1 }, 0, 1);
                await _stream.WriteAsync(lenBytes, 0, 4);
                await _stream.WriteAsync(textBytes, 0, textBytes.Length);
                await _stream.FlushAsync();

                LogReceived?.Invoke($"[Вы]: {text}");
            }
            catch (Exception ex)
            {
                LogReceived?.Invoke($"Ошибка отправки: {ex.Message}");
                Disconnect();
            }
        }

        public async Task SendFileAsync(string filePath)
        {
            if (!_isConnected || _stream == null) return;

            try
            {
                await FileTransfer.SendFileAsync(_stream, filePath);
                LogReceived?.Invoke($"[Вы] отправили файл: {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                LogReceived?.Invoke($"Ошибка отправки файла: {ex.Message}");
                Disconnect();
            }
        }

        private async Task ReceiveLoopAsync(CancellationToken token)
        {
            try
            {
                byte[] typeBuf = new byte[1];

                while (_isConnected && !token.IsCancellationRequested)
                {
                    int read = await _stream!.ReadAsync(typeBuf, 0, 1, token);
                    if (read == 0) break;

                    byte type = typeBuf[0];

                    if (type == 1)
                    {
                        string text = await ReadStringExactAsync(_stream, token);
                        LogReceived?.Invoke($"[Сервер]: {text}");
                    }
                    else if (type == 2)
                    {
                        string savedPath = await FileTransfer.ReceiveFileAsync(_stream, token);
                        LogReceived?.Invoke($"[Сервер] отправил файл: {Path.GetFileName(savedPath)} (сохранён)");
                    }
                }
            }
            catch { }
            finally
            {
                Disconnect();
            }
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

        public void Disconnect()
        {
            if (!_isConnected) return;

            _isConnected = false;
            _cts?.Cancel();
            _client?.Close();

            LogReceived?.Invoke("Отключено");
            Disconnected?.Invoke();
        }
    }
}