using System.Windows.Controls;
using System.Windows.Input;
using PropertyManager.ViewModels;

namespace PropertyManager.Views
{
    public partial class TenantView : UserControl
    {
        public TenantView() { InitializeComponent(); }

        private void FormField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is TenantViewModel vm)
            {
                vm.SaveTenantCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
