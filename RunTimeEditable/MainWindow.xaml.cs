using Microsoft.Scripting.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Markup;

namespace RuntimeEditable {
   public interface ElementTabSet {
      void AddTab(ElementTab tab);
      void RemoveTab(ScriptScope scope);
   }

   public class NullElementTabSet : ElementTabSet {
      public void AddTab(ElementTab tab) { }
      public void RemoveTab(ScriptScope scope) { }
   }

   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window, ElementTabSet {

      readonly FileSystemWatcher _watcher;
      readonly ScriptEngine _engine;
      readonly IList<ElementTab> _appTabs = new List<ElementTab>();

      public MainWindow() {
         _watcher = new FileSystemWatcher();
         _engine = IronRuby.Ruby.CreateEngine();
         InitializeComponent();
         _errors.TextChanged += (sender, e) => {
            if (string.IsNullOrEmpty(_errors.Text)) return;
            _tabs.SelectedIndex = 0;
         };
      }

      public void AddTab(ElementTab tab) {
         _appTabs.Add(tab);
         _tabs.Items.Add(tab);
      }

      public void RemoveTab(ScriptScope scope) {
         var index = _appTabs.IndexOf(_appTabs.First(tab => tab.Scope == scope));
         _tabs.Items.Remove(_appTabs[index]);
         _appTabs.RemoveAt(index);
      }

      protected override void OnInitialized(EventArgs e) {
         base.OnInitialized(e);
         _watcher.Path = AppDomain.CurrentDomain.BaseDirectory; // TODO
         // TODO do an initial sweep
         _watcher.IncludeSubdirectories = true;
         _watcher.NotifyFilter = NotifyFilters.LastWrite;
         _watcher.Created += OnChanged;
         _watcher.Changed += OnChanged;
         _watcher.Renamed += OnChanged;
         _watcher.EnableRaisingEvents = true;
      }

      protected override void OnClosed(EventArgs e) {
         base.OnClosed(e);
         Application.Current.Shutdown();
      }

      void OnChanged(object sender, FileSystemEventArgs e) {
         Dispatcher.Invoke(() => {
            _errors.Text = "";

            try {
               // if a tab can take it, assume we're good
               bool any = false;
               foreach (var tab in _appTabs.ToList()) {
                  any = tab.Refresh(e.FullPath.Substring(_watcher.Path.Length)) || any;
               }
               if (any) return;

               if (e.FullPath.EndsWith(".xaml")) {
                  UpdateXaml(e.FullPath);
               } else {
                  _errors.Text = "Could not find any xaml associated with " + e.FullPath + ".";
               }
            } catch (IOException) {
               // meh
            } catch (Exception e1) {
               _errors.Text = "Changes detected in " + e.FullPath + ", but there's an error." + Environment.NewLine + Environment.NewLine +
                  e1.Message + Environment.NewLine +
                  e1.StackTrace;
            }
         });
      }

      void UpdateXaml(string path) {
         var scope = Control.CreateScope(_watcher.Path, _engine);
         scope.OpenScope(new NullElementTabSet());
         var xaml = XamlReader.Parse(File.ReadAllText(path));
         scope.CloseScope();
         if (xaml is ResourceDictionary) {
            Application.Current.Resources = (ResourceDictionary)xaml;
            return;
         } else if (!(xaml is Window)) {
            _errors.Text = "Changes detected in " + path + ", but there seems to be nowhere to put the changes";
            return;
         }

         var source = path.Substring(_watcher.Path.Length, path.Length - _watcher.Path.Length - 5);
         var tab = new ElementTab(_engine.CreateScope(), _watcher.Path, source, this, true);
         _appTabs.Add(tab);
         tab.Refresh(source+".xaml");
         _tabs.Items.Add(tab);
         if (File.Exists(_watcher.Path + source + ".rb")) tab.Refresh(source + ".rb");
      }
   }
}
