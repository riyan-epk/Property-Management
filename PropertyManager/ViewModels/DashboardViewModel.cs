using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.EntityFrameworkCore;
using PropertyManager.Data;
using PropertyManager.Services;
using System.Linq;
using System.Threading.Tasks;

namespace PropertyManager.ViewModels
{
    /// <summary>
    /// Dashboard showing aggregate statistics.
    /// </summary>
    public partial class DashboardViewModel : ObservableObject
    {
        [ObservableProperty] private int _totalProperties;
        [ObservableProperty] private int _totalUnits;
        [ObservableProperty] private int _totalTenants;
        [ObservableProperty] private int _totalAgreements;
        [ObservableProperty] private decimal _totalRentCollected;
        [ObservableProperty] private decimal _totalPendingDues;
        [ObservableProperty] private int _occupiedUnits;
        [ObservableProperty] private int _vacantUnits;

        public DashboardViewModel()
        {
            LoadDataCommand.Execute(null);
        }

        [RelayCommand]
        private async Task LoadData()
        {
            await Task.Run(() =>
            {
                using var db = new AppDbContext();

                TotalProperties = db.Properties.Count();
                TotalUnits = db.Units.Count();
                TotalTenants = db.Tenants.Count();
                TotalAgreements = db.Agreements.Count();
                OccupiedUnits = db.Units.Count(u => u.Status == "Occupied");
                VacantUnits = db.Units.Count(u => u.Status == "Vacant");

                var agreements = db.Agreements
                    .Include(a => a.Expenses)
                    .Include(a => a.Payments)
                    .Include(a => a.Tenant)
                    .Include(a => a.Unit)
                    .ToList();

                TotalRentCollected = agreements
                    .SelectMany(a => a.Payments)
                    .Sum(p => p.PaidAmount);

                TotalPendingDues = agreements
                    .Sum(a => RentCalculationService.GetTotalOutstandingBalance(a));
            });
        }
    }
}
