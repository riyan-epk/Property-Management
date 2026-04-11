using System.Windows.Controls;
using System.Windows.Input;
using PropertyManager.ViewModels;

namespace PropertyManager.Views
{
    public partial class PaymentView : UserControl
    {
        public PaymentView() { InitializeComponent(); }

        private void FormField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is PaymentViewModel vm)
            {
                vm.SavePaymentCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
