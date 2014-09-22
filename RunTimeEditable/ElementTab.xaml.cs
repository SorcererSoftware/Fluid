using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace RuntimeEditable {
   public class Commands {
      public static readonly ICommand Evaluate = new RoutedCommand();
   }

   /// <summary>
   /// Interaction logic for ApplicationTab.xaml
   /// </summary>
   public partial class ElementTab : TabItem {
      public readonly ScriptScope Scope;

      readonly string _directory;
      readonly IControlScope _controlScope;
      readonly ElementTabSet _tabset;

      readonly bool _isWindow;
      public FrameworkElement Element { get; private set; }

      #region Source

      public string Source {
         get { return (string)GetValue(SourceProperty); }
         set { SetValue(SourceProperty, value); }
      }

      public static readonly DependencyProperty SourceProperty =
          DependencyProperty.Register("Source", typeof(string), typeof(ElementTab), new FrameworkPropertyMetadata(null, SourceChanged));

      static void SourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) {
         var self = (ElementTab)d;

         if (self.Source == null) return;

         if (File.Exists(self._directory + self.Source + ".xaml")) {
            self.Refresh(self.Source + ".xaml");
         }

         if (File.Exists(self._directory + self.Source + ".rb")) {
            self.Refresh(self.Source + ".rb");
         }
      }

      #endregion

      public ElementTab(ScriptScope scope, string directory, string source, ElementTabSet tabset, bool isWindow) {
         _isWindow = isWindow;
         _tabset = tabset;
         Scope = scope;
         _directory = directory;
         _controlScope = Control.CreateScope(_directory, scope.Engine);
         Scope.SetVariable("bindings", new BindingFactory());
         Scope.SetVariable("model", new ExpandoDependencyObject());
         DataContext = this;
         InitializeComponent();
         _variableList.ItemsSource = Scope.GetVariable("model");
         Source = source;
      }

      public bool Refresh(string name) {
         if (name == Source + ".xaml") {
            UpdateXaml();
            return true;
         }

         if (name == Source + ".rb") {
            UpdateRuby();
            return true;
         }

         return _controlScope.ObserveChange(name);
      }

      public IEnumerable<ScriptScope> Scopes() {
         return new[] { Scope }.Concat(_controlScope.Scopes());
      }

      void EvaluateExpression(object sender, EventArgs e) {
         Scope.Engine.Execute(_expression.Text, Scope);
      }

      void UpdateRuby() {
         var source = Scope.Engine.CreateScriptSourceFromFile(_directory + Source + ".rb");
         var connector = ((BindingFactory)Scope.GetVariable("bindings"));
         connector.Bindings.Clear();
         source.Execute(Scope);
         UpdateBindings();

         //try {
         //   // run the ruby
         //   var source = _scope.Engine.CreateScriptSourceFromFile(path);
         //   source.Execute(_scope);

         //   // find a window with the same name
         //   UpdateBindings();
         //} catch (IOException) {
         //   // meh, don't really care
         //} catch (Exception e1) {
         //   // main window still acts as an error window.
         //   _errors.Text = "Changes detected in " + path + ", but there's an error." + Environment.NewLine +
         //      e1.Message + Environment.NewLine +
         //      e1.StackTrace;
         //}
      }

      void UpdateXaml() {
         FrameworkElement element = null;
         Window window = null;

         try {
            _controlScope.OpenScope(_tabset);
            element = XamlReader.Parse(File.ReadAllText(_directory + Source + ".xaml")) as FrameworkElement;
            window = element as Window;
         } finally {
            _controlScope.CloseScope();
         }

         if (_isWindow) {
            Debug.Assert(window != null, "We parsed some xaml expecting a window, but then it wasn't a window...");
         }

         if (Element == null || !_isWindow || !Element.IsLoaded) {
            Element = element;
            Element.DataContext = Scope.GetVariable("model");
            if (_isWindow) window.Show();
         } else {
            var oldWindow = (Window)Element;
            oldWindow.Content = window.Content;
            oldWindow.Title = window.Title;
         }

         UpdateBindings();
      }

      void UpdateBindings() {
         var connector = ((BindingFactory)Scope.GetVariable("bindings"));
         var model = Scope.GetVariable("model");

         var bindings = connector.Bindings;
         var commandList = new List<ICommand>();
         foreach (CommandBinding binding in bindings) commandList.Add(binding.Command);
         _commandList.ItemsSource = commandList;

         // update debug bindings
         this.CommandBindings.Clear();
         this.CommandBindings.AddRange(bindings);

         if (Element == null) {
            CommandManager.InvalidateRequerySuggested();
            return;
         }

         // update window bindings
         Element.CommandBindings.Clear();
         Element.CommandBindings.AddRange(bindings);
         CommandManager.InvalidateRequerySuggested();
      }
   }
}
