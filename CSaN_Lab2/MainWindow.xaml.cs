using System.Windows;

namespace CSaN_Lab2
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void ServerButton_Click(object sender, RoutedEventArgs e)
        {
            new ServerWindow().Show();
        }

        private void ClientButton_Click(object sender, RoutedEventArgs e)
        {
            new ClientWindow().Show();
        }
    }
}