using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using textHandlerApp.ViewModels;

namespace textHandlerApp
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            DataContext = new MainViewModel();
        }

        private static readonly Regex NumberRegex = new Regex("[^0-9]+");
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            var regex = NumberRegex;
            e.Handled = regex.IsMatch(e.Text);
        }
    }
}