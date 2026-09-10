using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Data;
using PropertyManager.Models;
using PropertyManager.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PropertyManager.ViewModels
{
    public partial class AgreementViewModel : ObservableObject
    {
        [ObservableProperty] private ObservableCollection<Agreement> _agreements = new();
        [ObservableProperty] private ObservableCollection<Tenant> _tenants = new();
        [ObservableProperty] private ObservableCollection<Unit> _availableUnits = new();
        [ObservableProperty] private Agreement? _selectedAgreement;

        // ── Form Fields ──
        [ObservableProperty] private Tenant? _selectedTenant;
        [ObservableProperty] private Unit? _selectedUnit;
        [ObservableProperty] private DateTime _startDate = DateTime.Today;
        [ObservableProperty] private DateTime _endDate = DateTime.Today.AddYears(1);
        [ObservableProperty] private decimal _baseRent;
        [ObservableProperty] private IncreaseType _increaseType = IncreaseType.Yearly;
        [ObservableProperty] private int _increaseAfterMonths = 12;
        [ObservableProperty] private double _increasePercentage = 10;
        [ObservableProperty] private bool _isEditing;
        private int _editingId;

        public AgreementViewModel()
        {
            LoadDataCommand.Execute(null);
        }

        partial void OnIncreaseTypeChanged(IncreaseType value)
        {
            // Auto-set months based on type selection
            IncreaseAfterMonths = value switch
            {
                IncreaseType.Monthly => 1,
                IncreaseType.Yearly => 12,
                _ => IncreaseAfterMonths
            };
        }

        partial void OnSelectedUnitChanged(Unit? value)
        {
            if (value != null && !IsEditing)
            {
                BaseRent = value.BaseRent;
            }
        }

        [RelayCommand]
        private async Task LoadData()
        {
            await Task.Run(() =>
            {
                using var db = new AppDbContext();

                var agreementList = db.Agreements
                    .Include(a => a.Tenant)
                    .Include(a => a.Unit)
                        .ThenInclude(u => u!.Property)
                    .OrderByDescending(a => a.StartDate)
                    .ToList();

                var tenantList = db.Tenants.OrderBy(t => t.Name).ToList();
                var unitList = db.Units.Include(u => u.Property).OrderBy(u => u.UnitNumber).ToList();

                App.Current.Dispatcher.Invoke(() =>
                {
                    Agreements = new ObservableCollection<Agreement>(agreementList);
                    Tenants = new ObservableCollection<Tenant>(tenantList);
                    AvailableUnits = new ObservableCollection<Unit>(unitList);
                });
            });
        }

        [RelayCommand]
        private void SaveAgreement()
        {
            if (SelectedTenant == null)
            {
                MessageBox.Show("Select a tenant.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (SelectedUnit == null)
            {
                MessageBox.Show("Select a unit.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (StartDate >= EndDate)
            {
                MessageBox.Show("End date must be after start date.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (BaseRent <= 0)
            {
                MessageBox.Show("Base rent must be greater than zero.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new AppDbContext();

            if (IsEditing)
            {
                var agreement = db.Agreements.Find(_editingId);
                if (agreement != null)
                {
                    int oldUnitId = agreement.UnitId;

                    agreement.TenantId = SelectedTenant.Id;
                    agreement.UnitId = SelectedUnit.Id;
                    agreement.StartDate = StartDate;
                    agreement.EndDate = EndDate;
                    agreement.BaseRent = BaseRent;
                    agreement.IncreaseType = IncreaseType;
                    agreement.IncreaseAfterMonths = IncreaseAfterMonths;
                    agreement.IncreasePercentage = IncreasePercentage;
                    db.SaveChanges();

                    // Keep unit occupancy in sync when the unit is reassigned.
                    if (oldUnitId != SelectedUnit.Id)
                    {
                        var newUnit = db.Units.Find(SelectedUnit.Id);
                        if (newUnit != null) newUnit.Status = "Occupied";

                        var oldUnit = db.Units.Find(oldUnitId);
                        if (oldUnit != null &&
                            !db.Agreements.Any(a => a.UnitId == oldUnitId && a.Id != _editingId))
                        {
                            oldUnit.Status = "Vacant";
                        }
                        db.SaveChanges();
                    }
                }
                IsEditing = false;
            }
            else
            {
                var agreement = new Agreement
                {
                    TenantId = SelectedTenant.Id,
                    UnitId = SelectedUnit.Id,
                    StartDate = StartDate,
                    EndDate = EndDate,
                    BaseRent = BaseRent,
                    IncreaseType = IncreaseType,
                    IncreaseAfterMonths = IncreaseAfterMonths,
                    IncreasePercentage = IncreasePercentage
                };
                db.Agreements.Add(agreement);
                db.SaveChanges();

                // Mark unit as occupied
                var unit = db.Units.Find(SelectedUnit.Id);
                if (unit != null) { unit.Status = "Occupied"; db.SaveChanges(); }
            }

            ClearForm();
            LoadDataCommand.Execute(null);
        }

        [RelayCommand]
        private void EditAgreement()
        {
            if (SelectedAgreement == null) return;
            var a = SelectedAgreement;
            SelectedTenant = Tenants.FirstOrDefault(t => t.Id == a.TenantId);
            SelectedUnit = AvailableUnits.FirstOrDefault(u => u.Id == a.UnitId);
            StartDate = a.StartDate;
            EndDate = a.EndDate;
            BaseRent = a.BaseRent;
            IncreaseType = a.IncreaseType;
            IncreaseAfterMonths = a.IncreaseAfterMonths;
            IncreasePercentage = a.IncreasePercentage;
            _editingId = a.Id;
            IsEditing = true;
        }

        [RelayCommand]
        private void DeleteAgreement()
        {
            if (SelectedAgreement == null) return;
            if (MessageBox.Show("Delete this agreement?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            try
            {
                using var db = new AppDbContext();
                var agreement = db.Agreements.Find(SelectedAgreement.Id);
                if (agreement != null)
                {
                    int unitId = agreement.UnitId;
                    db.Agreements.Remove(agreement);
                    db.SaveChanges();

                    // Only free the unit if no other agreement still uses it.
                    var unit = db.Units.Find(unitId);
                    if (unit != null && !db.Agreements.Any(a => a.UnitId == unitId))
                    {
                        unit.Status = "Vacant";
                        db.SaveChanges();
                    }
                }
                LoadDataCommand.Execute(null);
            }
            catch (Exception)
            {
                MessageBox.Show(
                    "This agreement is already in use (has expenses or payments linked) and cannot be deleted.\n\nRemove all related records first.",
                    "Cannot Delete", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        [RelayCommand]
        private void CancelEdit()
        {
            IsEditing = false;
            ClearForm();
        }

        [RelayCommand]
        private void PrintAgreement()
        {
            if (SelectedAgreement == null)
            {
                MessageBox.Show("Select an agreement from the list to print.", "Print Agreement",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DocumentService.PrintAgreementDeed(SelectedAgreement);
        }

        private void ClearForm()
        {
            SelectedTenant = null;
            SelectedUnit = null;
            StartDate = DateTime.Today;
            EndDate = DateTime.Today.AddYears(1);
            BaseRent = 0;
            IncreaseType = IncreaseType.Yearly;
            IncreaseAfterMonths = 12;
            IncreasePercentage = 10;
        }
    }
}
