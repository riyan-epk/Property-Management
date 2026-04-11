using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using System;
using PropertyManager.Data;
using PropertyManager.Models;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PropertyManager.ViewModels
{
    public partial class PropertyViewModel : ObservableObject
    {
        // ── Observable Collections ──
        [ObservableProperty] private ObservableCollection<Property> _properties = new();
        [ObservableProperty] private ObservableCollection<Unit> _units = new();
        [ObservableProperty] private Property? _selectedProperty;

        // ── Property Form Fields ──
        [ObservableProperty] private string _propertyName = string.Empty;
        [ObservableProperty] private string _propertyLocation = string.Empty;
        [ObservableProperty] private string _propertyType = "Residential";
        [ObservableProperty] private bool _isEditingProperty;
        private int _editingPropertyId;

        // ── Unit Form Fields ──
        [ObservableProperty] private string _unitNumber = string.Empty;
        [ObservableProperty] private decimal _unitBaseRent;
        [ObservableProperty] private string _unitStatus = "Vacant";
        [ObservableProperty] private bool _isEditingUnit;
        private int _editingUnitId;

        public string[] PropertyTypes => new[] { "Residential", "Commercial", "Industrial", "Mixed" };
        public string[] UnitStatuses => new[] { "Vacant", "Occupied" };

        public PropertyViewModel()
        {
            LoadDataCommand.Execute(null);
        }

        partial void OnSelectedPropertyChanged(Property? value)
        {
            LoadUnitsForProperty();
        }

        [RelayCommand]
        private async Task LoadData()
        {
            await Task.Run(() =>
            {
                using var db = new AppDbContext();
                var list = db.Properties.Include(p => p.Units).ToList();
                App.Current.Dispatcher.Invoke(() =>
                {
                    Properties = new ObservableCollection<Property>(list);
                    if (Properties.Any()) SelectedProperty = Properties.First();
                });
            });
        }

        private void LoadUnitsForProperty()
        {
            if (SelectedProperty == null) { Units.Clear(); return; }
            using var db = new AppDbContext();
            var unitList = db.Units.Where(u => u.PropertyId == SelectedProperty.Id).ToList();
            Units = new ObservableCollection<Unit>(unitList);
        }

        // ── Property CRUD ──

        [RelayCommand]
        private void SaveProperty()
        {
            if (string.IsNullOrWhiteSpace(PropertyName))
            {
                MessageBox.Show("Property name is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new AppDbContext();

            if (IsEditingProperty)
            {
                var prop = db.Properties.Find(_editingPropertyId);
                if (prop != null)
                {
                    prop.Name = PropertyName;
                    prop.Location = PropertyLocation;
                    prop.Type = PropertyType;
                    db.SaveChanges();
                }
                IsEditingProperty = false;
            }
            else
            {
                db.Properties.Add(new Property
                {
                    Name = PropertyName,
                    Location = PropertyLocation,
                    Type = PropertyType
                });
                db.SaveChanges();
            }

            ClearPropertyForm();
            LoadDataCommand.Execute(null);
        }

        [RelayCommand]
        private void EditProperty()
        {
            if (SelectedProperty == null) return;
            PropertyName = SelectedProperty.Name;
            PropertyLocation = SelectedProperty.Location;
            PropertyType = SelectedProperty.Type;
            _editingPropertyId = SelectedProperty.Id;
            IsEditingProperty = true;
        }

        [RelayCommand]
        private void DeleteProperty()
        {
            if (SelectedProperty == null) return;
            if (MessageBox.Show($"Delete property '{SelectedProperty.Name}'?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                using var db = new AppDbContext();
                var prop = db.Properties.Find(SelectedProperty.Id);
                if (prop != null) { db.Properties.Remove(prop); db.SaveChanges(); }
                LoadDataCommand.Execute(null);
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "This property is already in use (has units or agreements linked) and cannot be deleted.\n\nRemove all related units and agreements first.",
                    "Cannot Delete", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        private void CancelEditProperty()
        {
            IsEditingProperty = false;
            ClearPropertyForm();
        }

        private void ClearPropertyForm()
        {
            PropertyName = string.Empty;
            PropertyLocation = string.Empty;
            PropertyType = "Residential";
        }

        // ── Unit CRUD ──

        [RelayCommand]
        private void SaveUnit()
        {
            if (SelectedProperty == null)
            {
                MessageBox.Show("Select a property first.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(UnitNumber))
            {
                MessageBox.Show("Unit number is required.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new AppDbContext();

            if (IsEditingUnit)
            {
                var unit = db.Units.Find(_editingUnitId);
                if (unit != null)
                {
                    unit.UnitNumber = UnitNumber;
                    unit.BaseRent = UnitBaseRent;
                    unit.Status = UnitStatus;
                    db.SaveChanges();
                }
                IsEditingUnit = false;
            }
            else
            {
                db.Units.Add(new Unit
                {
                    PropertyId = SelectedProperty.Id,
                    UnitNumber = UnitNumber,
                    BaseRent = UnitBaseRent,
                    Status = UnitStatus
                });
                db.SaveChanges();
            }

            ClearUnitForm();
            LoadUnitsForProperty();
        }

        [RelayCommand]
        private void EditUnit(Unit unit)
        {
            if (unit == null) return;
            UnitNumber = unit.UnitNumber;
            UnitBaseRent = unit.BaseRent;
            UnitStatus = unit.Status;
            _editingUnitId = unit.Id;
            IsEditingUnit = true;
        }

        [RelayCommand]
        private void DeleteUnit(Unit unit)
        {
            if (unit == null) return;
            if (MessageBox.Show($"Delete unit '{unit.UnitNumber}'?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                using var db = new AppDbContext();
                var u = db.Units.Find(unit.Id);
                if (u != null) { db.Units.Remove(u); db.SaveChanges(); }
                LoadUnitsForProperty();
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "This unit is already in use (has agreements linked) and cannot be deleted.\n\nRemove all related agreements first.",
                    "Cannot Delete", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        private void CancelEditUnit()
        {
            IsEditingUnit = false;
            ClearUnitForm();
        }

        private void ClearUnitForm()
        {
            UnitNumber = string.Empty;
            UnitBaseRent = 0;
            UnitStatus = "Vacant";
        }
    }
}
