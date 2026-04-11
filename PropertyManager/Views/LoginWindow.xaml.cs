using System.Windows;
using System.Windows.Input;
using PropertyManager.ViewModels;

namespace PropertyManager.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            DragMove();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }

        /// <summary>
        /// Passes the PasswordBox.Password directly to the ViewModel since 
        /// PasswordBox.Password is NOT a DependencyProperty and cannot be bound in XAML.
        /// </summary>
        private void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel vm)
            {
                vm.LoginCommand.Execute(PasswordBox.Password);
            }
        }

        private void PasswordBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                LoginButton_Click(sender, e);
                e.Handled = true;
            }
        }
    }
}
