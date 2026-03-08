using CSaN_Lab2.Logic;
using Microsoft.Win32;
using System;
using System.Windows;

namespace CSaN_Lab2
{
    public partial class ServerWindow : Window
    {
        private readonly Server _server = new Server();

        public ServerWindow()
        {
            InitializeComponent();

            _server.LogReceived += msg => Dispatcher.Invoke(() => AppendToChat(msg));
            _server.ClientConnected += ip => Dispatcher.Invoke(() => AppendToChat($"Подключился: {ip}"));
            _server.ClientDisconnected += ip => Dispatcher.Invoke(() => AppendToChat($"Отключился: {ip}"));
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            string ip = txtIP.Text.Trim();
            if (!int.TryParse(txtPort.Text, out int port) || port < 1 || port > 65535)
            {
                MessageBox.Show("Введите корректный порт (1–65535)", "Ошибка");
                return;
            }

            btnStart.IsEnabled = false;
            await _server.StartAsync(ip, port);
            btnStart.IsEnabled = true;
        }

        private void Stop_Click(object sender, RoutedEventArgs e)
        {
            _server.Stop();
        }

        private async void SendMessage_Click(object sender, RoutedEventArgs e)
        {
            string targetIp = txtTargetIP.Text.Trim();
            string text = txtMessage.Text.Trim();

            if (string.IsNullOrWhiteSpace(targetIp))
            {
                MessageBox.Show("Введите IP получателя", "Ошибка");
                return;
            }

            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("Введите текст сообщения", "Ошибка");
                return;
            }

            await _server.SendToClientAsync(targetIp, text);
            txtMessage.Clear();
        }

        private void AppendToChat(string message)
        {
            txtChat.AppendText($"{DateTime.Now:HH:mm:ss}  {message}\n");
            txtChat.ScrollToEnd();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _server.Stop();
        }

      
        private async void SendFile_Click(object sender, RoutedEventArgs e)
        {
            string targetIp = txtTargetIP.Text.Trim();

            if (string.IsNullOrWhiteSpace(targetIp))
            {
                MessageBox.Show("Введите IP получателя", "Ошибка");
                return;
            }

            var dialog = new OpenFileDialog
            {
                Title = "Выберите файл для отправки",
                Filter = "Все файлы (*.*)|*.*"
            };

            if (dialog.ShowDialog() == true)
            {
                await _server.SendFileToClientAsync(targetIp, dialog.FileName);
            }
        }
    }
}