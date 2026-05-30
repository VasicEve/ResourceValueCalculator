using System.Windows;

namespace ResourceValueCalculator
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var vm = new MainViewModel();
            DataContext = vm;
            Loaded += async (_, _) => await vm.LoadCatalogAsync();
        }
    }
}
