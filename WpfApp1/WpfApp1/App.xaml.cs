using System.Configuration;
using System.Data;
using System.Windows;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            var dataService = new DataService();
            var userService = new UserService();
            // другие сервисы

            Current.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel(dataService, userService)
            };

            Current.MainWindow.Show();
        }
    }

}
