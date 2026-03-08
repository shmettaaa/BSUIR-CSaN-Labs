using CSaN_Lab2.Logic;
using Microsoft.Win32;
using System;
using System.Windows;

namespace CSaN_Lab2
{
    public partial class ClientWindow : Window
    {
        private readonly Client _client;

        public ClientWindow()
        {
            InitializeComponent();
            _client = new Client();

            _client.LogReceived += OnClientLog;
            _client.Connected += OnConnected;
            _client.Disconnected += OnDisconnected;
        }

        private void OnClientLog(string msg) => Dispatcher.Invoke(() => AppendToChat(msg));
        private void OnConnected() => Dispatcher.Invoke(() =>
        {
            AppendToChat("Успешно подключено!");
            btnConnect.IsEnabled = false;
            btnDisconnect.IsEnabled = true;
        });
        private void OnDisconnected() => Dispatcher.Invoke(() =>
        {
            AppendToChat("Отключено");
            btnConnect.IsEnabled = true;
            btnDisconnect.IsEnabled = false;
        });

        private async void Connect_Click(object sender, RoutedEventArgs e)
        {
            string ip = txtIP.Text.Trim();
            if (!int.TryParse(txtPort.Text, out int port))
            {
                MessageBox.Show("Неверный порт");
                return;
            }

            await _client.ConnectAsync(ip, port);
        }

        private void Disconnect_Click(object sender, RoutedEventArgs e) => _client.Disconnect();

        private async void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            string text = txtMessage.Text.Trim();
            if (string.IsNullOrWhiteSpace(text)) return;

            await _client.SendTextAsync(text);
            txtMessage.Clear();
        }

        private async void SendFile_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = "Выберите файл",
                Filter = "Все файлы (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                await _client.SendFileAsync(dialog.FileName);
            }
        }
        private void AppendToChat(string message)
        {
            txtChat.AppendText($"{DateTime.Now:HH:mm:ss}  {message}\n");
            txtChat.ScrollToEnd();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _client.Disconnect();
        }
    }
}