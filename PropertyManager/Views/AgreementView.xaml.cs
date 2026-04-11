using System.Windows.Controls;
using System.Windows.Input;
using PropertyManager.ViewModels;

namespace PropertyManager.Views
{
    public partial class AgreementView : UserControl
    {
        public AgreementView() { InitializeComponent(); }

        private void FormField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is AgreementViewModel vm)
            {
                vm.SaveAgreementCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
