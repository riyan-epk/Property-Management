using System.Windows.Controls;
using System.Windows.Input;
using PropertyManager.ViewModels;

namespace PropertyManager.Views
{
    public partial class PropertyView : UserControl
    {
        public PropertyView() { InitializeComponent(); }

        private void PropertyField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is PropertyViewModel vm)
            {
                vm.SavePropertyCommand.Execute(null);
                e.Handled = true;
            }
        }

        private void UnitField_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && DataContext is PropertyViewModel vm)
            {
                vm.SaveUnitCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
