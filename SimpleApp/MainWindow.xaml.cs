using System.Windows;

namespace SimpleApp {
   public partial class MainWindow : Window {
      public MainWindow() {
         var viewModel = new ViewModel();
         DataContext = viewModel;
         CommandBindings.Add(viewModel.ConvertBinding);
         InitializeComponent();
      }
   }
}
