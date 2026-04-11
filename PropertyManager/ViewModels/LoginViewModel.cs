using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

namespace PropertyManager.ViewModels
{
    /// <summary>
    /// Login ViewModel with hardcoded credentials.
    /// </summary>
    public partial class LoginViewModel : ObservableObject
    {
        [ObservableProperty] private string _username = string.Empty;
        [ObservableProperty] private string _errorMessage = string.Empty;
        [ObservableProperty] private bool _hasError;

        // Password is handled via PasswordBox (not bindable for security);
        // the View passes it as a command parameter.

        private const string ValidUsername = "admin";
        private const string ValidPassword = "admin123";

        [RelayCommand]
        private void Login(object? parameter)
        {
            string password = parameter as string ?? string.Empty;

            if (string.IsNullOrWhiteSpace(Username))
            {
                ErrorMessage = "Username is required.";
                HasError = true;
                return;
            }
            if (string.IsNullOrWhiteSpace(password))
            {
                ErrorMessage = "Password is required.";
                HasError = true;
                return;
            }

            if (Username == ValidUsername && password == ValidPassword)
            {
                HasError = false;

                // Open main window and close login
                var mainWindow = new MainWindow();
                mainWindow.Show();

                // Close the login window
                foreach (Window window in Application.Current.Windows)
                {
                    if (window is Views.LoginWindow)
                    {
                        window.Close();
                        break;
                    }
                }
            }
            else
            {
                ErrorMessage = "Invalid username or password.";
                HasError = true;
            }
        }
    }
}
