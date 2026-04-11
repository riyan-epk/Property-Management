using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace PropertyManager.ViewModels
{
    /// <summary>
    /// Main ViewModel handling sidebar navigation between screens.
    /// </summary>
    public partial class MainViewModel : ObservableObject
    {
        [ObservableProperty]
        private ObservableObject _currentViewModel;

        [ObservableProperty]
        private string _currentViewTitle = "Dashboard";

        public MainViewModel()
        {
            _currentViewModel = new DashboardViewModel();
        }

        [RelayCommand]
        private void NavigateToDashboard()
        {
            CurrentViewModel = new DashboardViewModel();
            CurrentViewTitle = "Dashboard";
        }

        [RelayCommand]
        private void NavigateToProperties()
        {
            CurrentViewModel = new PropertyViewModel();
            CurrentViewTitle = "Property Management";
        }

        [RelayCommand]
        private void NavigateToTenants()
        {
            CurrentViewModel = new TenantViewModel();
            CurrentViewTitle = "Tenant Management";
        }

        [RelayCommand]
        private void NavigateToAgreements()
        {
            CurrentViewModel = new AgreementViewModel();
            CurrentViewTitle = "Agreements";
        }

        [RelayCommand]
        private void NavigateToExpenses()
        {
            CurrentViewModel = new ExpenseViewModel();
            CurrentViewTitle = "Rent & Expenses";
        }

        [RelayCommand]
        private void NavigateToPayments()
        {
            CurrentViewModel = new PaymentViewModel();
            CurrentViewTitle = "Payments";
        }

        [RelayCommand]
        private void NavigateToReports()
        {
            CurrentViewModel = new ReportViewModel();
            CurrentViewTitle = "Reports";
        }

        [RelayCommand]
        private void NavigateToAbout()
        {
            CurrentViewModel = new AboutViewModel();
            CurrentViewTitle = "About Developer";
        }
    }
}
