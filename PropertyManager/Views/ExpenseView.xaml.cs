using System.Windows.Controls;
using System.Windows.Input;
using PropertyManager.ViewModels;

namespace PropertyManager.Views
{
    public partial class ExpenseView : UserControl
    {
        public ExpenseView() { InitializeComponent(); }

        private void FormField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is ExpenseViewModel vm)
            {
                vm.SaveExpenseCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
