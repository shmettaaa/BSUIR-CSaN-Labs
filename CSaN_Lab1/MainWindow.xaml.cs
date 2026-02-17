using System;
using System.Threading.Tasks;
using System.Windows;

namespace CSaN_Lab1
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            ShowLocalComputerInfo();
        }

        private void ShowLocalComputerInfo()
        {
            NetworkScanner scanner = new NetworkScanner();
            NetworkDevice localInfo = scanner.GetLocalComputerInfo();

            if (localInfo != null)
            {
                MyComputerInfoText.Text = $"Имя: {localInfo.HostName} | IP: {localInfo.IpAddress} | MAC: {localInfo.MacAddress}";
            }
            else
            {
                MyComputerInfoText.Text = "Не удалось получить информацию о локальном компьютере";
            }
        }

        private async void ScanButton_Click(object sender, RoutedEventArgs e)
        {
            ScanButton.IsEnabled = false;
            ScanProgress.Visibility = Visibility.Visible;
            StatusText.Text = "Сканирование...";
            DevicesGrid.Items.Clear();

            try
            {
                NetworkScanner scanner = new NetworkScanner();
                List<NetworkDevice> devices = await scanner.ScanAllLocalNetworksAsync();

                foreach (NetworkDevice device in devices)
                {
                    DevicesGrid.Items.Add(device);
                }

                StatusText.Text = "Найдено устройств: " + devices.Count;
            }
            catch (Exception ex)
            {
                StatusText.Text = "Ошибка: " + ex.Message;
            }
            finally
            {
                ScanProgress.Visibility = Visibility.Collapsed;
                ScanButton.IsEnabled = true;
            }
        }

        private void RefreshMyInfo_Click(object sender, RoutedEventArgs e)
        {
            ShowLocalComputerInfo();
        }
    }
}