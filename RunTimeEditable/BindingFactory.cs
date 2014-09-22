using System;
using System.Windows;
using System.Windows.Input;

namespace RuntimeEditable {
   class BindingFactory {
      public readonly CommandBindingCollection Bindings = new CommandBindingCollection();

      public void Add(string command, Action<RoutedEventArgs> action) {
         Bindings.Add(new CommandBinding(CommandExtension.CommandForString(command), (sender, e) => action(e)));
      }

      public void Add(string command, Action<RoutedEventArgs> action, Func<RoutedEventArgs, bool> func) {
         Bindings.Add(new CommandBinding(CommandExtension.CommandForString(command), (sender, e) => action(e), (sender, e) => e.CanExecute = func(e)));
      }
   }
}
