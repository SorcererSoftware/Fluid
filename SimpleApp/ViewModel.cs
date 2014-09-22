using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;

namespace SimpleApp {
   class ViewModel : DependencyObject {
      public static ICommand Convert = new RoutedCommand();

      Model _model = new Model();

      public ViewModel() {
         Results = new ObservableCollection<string>();
         ConvertBinding = new CommandBinding(Convert, ExecuteConvert, CanConvert);
         _model.Original = Input;
      }

      public ObservableCollection<string> Results { get; private set; }

      public CommandBinding ConvertBinding { get; private set; }

      #region property Input

      public string Input {
         get { return (string)GetValue(InputProperty); }
         set { SetValue(InputProperty, value); }
      }

      public static readonly DependencyProperty InputProperty = DependencyProperty.Register(
         "Input",
         typeof(string),
         typeof(ViewModel),
         new FrameworkPropertyMetadata("Enter some text to convert and press enter.", InputChanged));

      static void InputChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         var self = (ViewModel)d;
         self._model.Original = self.Input;
         CommandManager.InvalidateRequerySuggested();
      }

      #endregion

      void ExecuteConvert(object sender, EventArgs e) {
         var result = _model.SecretLogic();
         Results.Add(result);
      }

      void CanConvert(object sender, CanExecuteRoutedEventArgs e) {
         e.CanExecute = !string.IsNullOrEmpty(_model.Original);
      }
   }
}
