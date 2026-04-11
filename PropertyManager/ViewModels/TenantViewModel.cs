using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System;
using PropertyManager.Data;
using PropertyManager.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PropertyManager.ViewModels
{
    public partial class TenantViewModel : ObservableObject
    {
        [ObservableProperty] private ObservableCollection<Tenant> _tenants = new();
        [ObservableProperty] private Tenant? _selectedTenant;

        // ── Form Fields ──
        [ObservableProperty] private string _tenantName = string.Empty;
        [ObservableProperty] private string _tenantPhone = string.Empty;
        [ObservableProperty] private string _tenantCNIC = string.Empty;
        [ObservableProperty] private bool _isEditing;
        private int _editingId;

        public TenantViewModel()
        {
            LoadDataCommand.Execute(null);
        }

        [RelayCommand]
        private async Task LoadData()
        {
            await Task.Run(() =>
            {
                using var db = new AppDbContext();
                var list = db.Tenants.ToList();
                App.Current.Dispatcher.Invoke(() =>
                {
                    Tenants = new ObservableCollection<Tenant>(list);
                });
            });
        }

        [RelayCommand]
        private void SaveTenant()
        {
            if (string.IsNullOrWhiteSpace(TenantName))
            {
                MessageBox.Show("Tenant name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new AppDbContext();

            if (IsEditing)
            {
                var tenant = db.Tenants.Find(_editingId);
                if (tenant != null)
                {
                    tenant.Name = TenantName;
                    tenant.Phone = TenantPhone;
                    tenant.CNIC = TenantCNIC;
                    db.SaveChanges();
                }
                IsEditing = false;
            }
            else
            {
                db.Tenants.Add(new Tenant
                {
                    Name = TenantName,
                    Phone = TenantPhone,
                    CNIC = TenantCNIC
                });
                db.SaveChanges();
            }

            ClearForm();
            LoadDataCommand.Execute(null);
        }

        [RelayCommand]
        private void EditTenant()
        {
            if (SelectedTenant == null) return;
            TenantName = SelectedTenant.Name;
            TenantPhone = SelectedTenant.Phone;
            TenantCNIC = SelectedTenant.CNIC;
            _editingId = SelectedTenant.Id;
            IsEditing = true;
        }

        [RelayCommand]
        private void DeleteTenant()
        {
            if (SelectedTenant == null) return;
            if (MessageBox.Show($"Delete tenant '{SelectedTenant.Name}'?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                using var db = new AppDbContext();
                var tenant = db.Tenants.Find(SelectedTenant.Id);
                if (tenant != null) { db.Tenants.Remove(tenant); db.SaveChanges(); }
                LoadDataCommand.Execute(null);
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "This tenant is already in use (has agreements linked) and cannot be deleted.\n\nRemove all related agreements first.",
                    "Cannot Delete", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        private void CancelEdit()
        {
            IsEditing = false;
            ClearForm();
        }

        private void ClearForm()
        {
            TenantName = string.Empty;
            TenantPhone = string.Empty;
            TenantCNIC = string.Empty;
        }
    }
}
