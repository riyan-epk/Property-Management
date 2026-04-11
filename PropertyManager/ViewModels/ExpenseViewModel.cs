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
    public partial class ExpenseViewModel : ObservableObject
    {
        [ObservableProperty] private ObservableCollection<Agreement> _agreements = new();
        [ObservableProperty] private ObservableCollection<Expense> _expenses = new();
        [ObservableProperty] private Agreement? _selectedAgreement;

        // ── Form Fields ──
        [ObservableProperty] private string _month = DateTime.Now.ToString("yyyy-MM");
        [ObservableProperty] private decimal _electricity;
        [ObservableProperty] private decimal _maintenance;
        [ObservableProperty] private decimal _other;

        // ── Computed Display ──
        [ObservableProperty] private decimal _effectiveRent;
        [ObservableProperty] private decimal _totalExpenses;
        [ObservableProperty] private decimal _totalRent;

        [ObservableProperty] private bool _isEditing;
        private int _editingId;

        public ExpenseViewModel()
        {
            LoadDataCommand.Execute(null);
        }

        partial void OnSelectedAgreementChanged(Agreement? value)
        {
            LoadExpensesForAgreement();
            RecalculateRent();
        }

        partial void OnMonthChanged(string value) => RecalculateRent();
        partial void OnElectricityChanged(decimal value) => RecalculateRent();
        partial void OnMaintenanceChanged(decimal value) => RecalculateRent();
        partial void OnOtherChanged(decimal value) => RecalculateRent();

        private void RecalculateRent()
        {
            if (SelectedAgreement == null || string.IsNullOrEmpty(Month))
            {
                EffectiveRent = 0; TotalExpenses = 0; TotalRent = 0;
                return;
            }

            EffectiveRent = RentCalculationService.CalculateEffectiveRent(SelectedAgreement, Month);
            TotalExpenses = Electricity + Maintenance + Other;
            TotalRent = EffectiveRent + TotalExpenses;
        }

        [RelayCommand]
        private async Task LoadData()
        {
            await Task.Run(() =>
            {
                using var db = new AppDbContext();
                var list = db.Agreements
                    .Include(a => a.Tenant)
                    .Include(a => a.Unit)
                    .Include(a => a.Expenses)
                    .Include(a => a.Payments)
                    .OrderByDescending(a => a.StartDate)
                    .ToList();

                App.Current.Dispatcher.Invoke(() =>
                {
                    Agreements = new ObservableCollection<Agreement>(list);
                });
            });
        }

        private void LoadExpensesForAgreement()
        {
            if (SelectedAgreement == null) { Expenses.Clear(); return; }
            using var db = new AppDbContext();
            var list = db.Expenses
                .Where(e => e.AgreementId == SelectedAgreement.Id)
                .OrderByDescending(e => e.Month)
                .ToList();
            Expenses = new ObservableCollection<Expense>(list);
        }

        [RelayCommand]
        private void SaveExpense()
        {
            if (SelectedAgreement == null)
            {
                MessageBox.Show("Select an agreement first.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (string.IsNullOrWhiteSpace(Month) || Month.Length != 7)
            {
                MessageBox.Show("Enter a valid month (YYYY-MM).", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new AppDbContext();

            if (IsEditing)
            {
                var exp = db.Expenses.Find(_editingId);
                if (exp != null)
                {
                    exp.Month = Month;
                    exp.Electricity = Electricity;
                    exp.Maintenance = Maintenance;
                    exp.Other = Other;
                    db.SaveChanges();
                }
                IsEditing = false;
            }
            else
            {
                // Check for duplicate month
                if (db.Expenses.Any(e => e.AgreementId == SelectedAgreement.Id && e.Month == Month))
                {
                    MessageBox.Show("Expenses for this month already exist. Edit the existing record.", "Duplicate", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                db.Expenses.Add(new Expense
                {
                    AgreementId = SelectedAgreement.Id,
                    Month = Month,
                    Electricity = Electricity,
                    Maintenance = Maintenance,
                    Other = Other
                });
                db.SaveChanges();
            }

            ClearForm();
            LoadExpensesForAgreement();
        }

        [RelayCommand]
        private void EditExpense(Expense expense)
        {
            if (expense == null) return;
            Month = expense.Month;
            Electricity = expense.Electricity;
            Maintenance = expense.Maintenance;
            Other = expense.Other;
            _editingId = expense.Id;
            IsEditing = true;
        }

        [RelayCommand]
        private void DeleteExpense(Expense expense)
        {
            if (expense == null) return;
            if (MessageBox.Show("Delete this expense record?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            using var db = new AppDbContext();
            var exp = db.Expenses.Find(expense.Id);
            if (exp != null) { db.Expenses.Remove(exp); db.SaveChanges(); }
            LoadExpensesForAgreement();
        }

        [RelayCommand]
        private void CancelEdit()
        {
            IsEditing = false;
            ClearForm();
        }

        private void ClearForm()
        {
            Month = DateTime.Now.ToString("yyyy-MM");
            Electricity = 0;
            Maintenance = 0;
            Other = 0;
        }
    }
}
