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
    public partial class PaymentViewModel : ObservableObject
    {
        [ObservableProperty] private ObservableCollection<Agreement> _agreements = new();
        [ObservableProperty] private ObservableCollection<Payment> _payments = new();
        [ObservableProperty] private ObservableCollection<MonthlyRentSummary> _monthlySummaries = new();
        [ObservableProperty] private Agreement? _selectedAgreement;

        // ── Form Fields ──
        [ObservableProperty] private string _month = DateTime.Now.ToString("yyyy-MM");
        [ObservableProperty] private decimal _paidAmount;
        [ObservableProperty] private DateTime _paymentDate = DateTime.Today;

        // ── Display ──
        [ObservableProperty] private decimal _totalOutstanding;
        [ObservableProperty] private decimal _monthlyRentForSelected;

        [ObservableProperty] private bool _isEditing;
        private int _editingId;

        public PaymentViewModel()
        {
            LoadDataCommand.Execute(null);
        }

        partial void OnSelectedAgreementChanged(Agreement? value)
        {
            LoadPayments();
            RecalculateSummaries();
        }

        partial void OnMonthChanged(string value)
        {
            if (SelectedAgreement != null && !string.IsNullOrEmpty(value) && value.Length == 7)
            {
                MonthlyRentForSelected = RentCalculationService.CalculateTotalRent(SelectedAgreement, value);
            }
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

        private void LoadPayments()
        {
            if (SelectedAgreement == null) { Payments.Clear(); return; }
            using var db = new AppDbContext();
            var list = db.Payments
                .Where(p => p.AgreementId == SelectedAgreement.Id)
                .OrderByDescending(p => p.Month)
                .ToList();
            Payments = new ObservableCollection<Payment>(list);
        }

        private void RecalculateSummaries()
        {
            if (SelectedAgreement == null)
            {
                MonthlySummaries.Clear();
                TotalOutstanding = 0;
                return;
            }

            // Reload agreement with navigation properties
            using var db = new AppDbContext();
            var agreement = db.Agreements
                .Include(a => a.Expenses)
                .Include(a => a.Payments)
                .FirstOrDefault(a => a.Id == SelectedAgreement.Id);

            if (agreement == null) return;

            var summaries = RentCalculationService.GetMonthlySummaries(agreement);
            MonthlySummaries = new ObservableCollection<MonthlyRentSummary>(summaries);
            TotalOutstanding = RentCalculationService.GetTotalOutstandingBalance(agreement);
        }

        [RelayCommand]
        private void SavePayment()
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
            if (PaidAmount <= 0)
            {
                MessageBox.Show("Paid amount must be greater than zero.", "Validation", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using var db = new AppDbContext();

            if (IsEditing)
            {
                var pmt = db.Payments.Find(_editingId);
                if (pmt != null)
                {
                    pmt.Month = Month;
                    pmt.PaidAmount = PaidAmount;
                    pmt.PaymentDate = PaymentDate;
                    db.SaveChanges();
                }
                IsEditing = false;
            }
            else
            {
                db.Payments.Add(new Payment
                {
                    AgreementId = SelectedAgreement.Id,
                    Month = Month,
                    PaidAmount = PaidAmount,
                    PaymentDate = PaymentDate
                });
                db.SaveChanges();
            }

            ClearForm();
            LoadPayments();
            RecalculateSummaries();
        }

        [RelayCommand]
        private void EditPayment(Payment payment)
        {
            if (payment == null) return;
            Month = payment.Month;
            PaidAmount = payment.PaidAmount;
            PaymentDate = payment.PaymentDate;
            _editingId = payment.Id;
            IsEditing = true;
        }

        [RelayCommand]
        private void DeletePayment(Payment payment)
        {
            if (payment == null) return;
            if (MessageBox.Show("Delete this payment?", "Confirm",
                MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;

            using var db = new AppDbContext();
            var pmt = db.Payments.Find(payment.Id);
            if (pmt != null) { db.Payments.Remove(pmt); db.SaveChanges(); }
            LoadPayments();
            RecalculateSummaries();
        }

        [RelayCommand]
        private void PrintReceipt(Payment payment)
        {
            if (payment == null || SelectedAgreement == null)
            {
                MessageBox.Show("Select an agreement and a payment to print a receipt.", "Rent Receipt",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            DocumentService.PrintRentReceipt(SelectedAgreement, payment);
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
            PaidAmount = 0;
            PaymentDate = DateTime.Today;
        }
    }
}
