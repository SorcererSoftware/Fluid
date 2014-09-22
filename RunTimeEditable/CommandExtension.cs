using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Input;
using System.Windows.Markup;

namespace RuntimeEditable {
   public class CommandExtension : MarkupExtension {
      static readonly IDictionary<string, RoutedCommand> _commands = new Dictionary<string, RoutedCommand>();

      readonly string _name;

      public CommandExtension(string name) {
         _name = name;
         if (!_commands.ContainsKey(_name)) _commands[_name] = new RoutedCommand(name, typeof(FrameworkElement));
      }

      public static ICommand CommandForString(string name) {
         if (!_commands.ContainsKey(name)) _commands[name] = new RoutedCommand(name, typeof(FrameworkElement));
         return _commands[name];
      }

      public static ICollection<RoutedCommand> AllCommands { get { return _commands.Values; } }

      public override object ProvideValue(IServiceProvider serviceProvider) {
         return _commands[_name];
      }
   }
}
