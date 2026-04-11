using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using System;
using System.IO;
using System.Windows;

namespace PropertyManager.ViewModels
{
    public partial class AboutViewModel : ObservableObject
    {
        [ObservableProperty]
        private string _developerName = "Riyan";

        [ObservableProperty]
        private string _role = ".NET Developer";

        [ObservableProperty]
        private string _contactNumber = "03098480389";

        public AboutViewModel()
        {
        }

        private string GetDbPath() => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "PropertyManager.db");

        [RelayCommand]
        private void BackupDatabase()
        {
            try
            {
                var dbPath = GetDbPath();
                if (!File.Exists(dbPath))
                {
                    MessageBox.Show("Database file not found.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                var dlg = new SaveFileDialog
                {
                    Title = "Backup Database",
                    Filter = "SQLite Database (*.db)|*.db",
                    FileName = $"PropertyManager_Backup_{DateTime.Now:yyyyMMdd_HHmmss}.db"
                };

                if (dlg.ShowDialog() == true)
                {
                    File.Copy(dbPath, dlg.FileName, true);
                    MessageBox.Show("Database backed up successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Backup failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        [RelayCommand]
        private void RestoreDatabase()
        {
            try
            {
                var dlg = new OpenFileDialog
                {
                    Title = "Restore Database",
                    Filter = "SQLite Database (*.db)|*.db"
                };

                if (dlg.ShowDialog() == true)
                {
                    var result = MessageBox.Show(
                        "Are you sure you want to restore the database? This will overwrite your current data.\n" +
                        "Note: It is strongly recommended to RESTART the application after restoring.", 
                        "Confirm Restore", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                        
                    if (result == MessageBoxResult.Yes)
                    {
                        var dbPath = GetDbPath();
                        File.Copy(dlg.FileName, dbPath, true);
                        MessageBox.Show("Database restored successfully! Please restart the application to see the changes.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (IOException ioEx)
            {
                MessageBox.Show($"Restore failed. Database is currently in use. Please restart the app and try immediately, or close other screens.\nError: {ioEx.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Restore failed: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
