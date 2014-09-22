using Microsoft.Scripting.Hosting;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

// TODO organize fields/properties list based on owning class
// TODO call methods
// TODO better auto-casting of MutableString to String
// TODO make .bat script to unsign dll's and exe's
// TODO make script reader: run script using the current scope.
// TODO make some kind of recursive search that
//          seeks an object given a type, 
//       or seeks an object given a name
//       or seeks an object given a value

namespace Snoop {
   public class RubyCommands {
      public static ICommand Enter = new RoutedCommand();
   }

   partial class SnoopUI {
      ScriptScope _scope = IronRuby.Ruby.CreateEngine().CreateScope();

      public void SetupRuby() {
         Tree.SelectedItemChanged += RubyNotifySelectedItemChanged;
         rubyInput.CommandBindings.Add(new CommandBinding(RubyCommands.Enter, ExecuteRuby));

         try {
            var script = @"
require 'WindowsBase'
require 'PresentationFramework'
require 'PresentationCore'
include System
include System::Windows
include System::Windows::Controls
include System::Windows::Media
include System::Windows::Shapes
            ";
            _scope.Engine.Execute(script, _scope);
         } catch (Exception e1) {
            rubyResults.Foreground = Brushes.Red;
            rubyResults.Text = e1.Message + Environment.NewLine + e1.StackTrace;
         }

         _scope.SetVariable("helper", new SnoopHelper());
      }

      void RubyNotifySelectedItemChanged(object sender, EventArgs e) {
         var item = this.CurrentSelection;
         _scope.SetVariable("current", item.MainVisual);
      }

      void ExecuteRuby(object sender, EventArgs e) {
         var source = rubyInput.Text;
         try {
            object result = _scope.Engine.Execute(source, _scope);
            var resultText = ProcessObject(result, 0);
            rubyResults.Text = resultText;
            rubyResults.Foreground = Brushes.Black;
         } catch (Exception e1) {
            rubyResults.Foreground = Brushes.Red;
            rubyResults.Text = e1.Message + Environment.NewLine + e1.StackTrace;
         }
      }

      string ProcessObject(object info, int tabLevel) {
         if (info == null) return "<nil>" + Environment.NewLine;
         string processed = "";
         string tabs = new string(' ', tabLevel * 3);
         string tab = "   ";
         if (info is string) {
            return (string)info + Environment.NewLine;
         } else if (info is IDictionary) {
            var dict = (IDictionary)info;
            processed += tabs + info.GetType().ToString() + " {" + Environment.NewLine;
            foreach (var key in dict.Keys) {
               processed += tabs + tab + key.ToString() + " : " + ProcessObject(dict[key], tabLevel + 2);
            }
            processed += tabs + "}" + Environment.NewLine;
         } else if (info is IEnumerable) {
            processed += tabs + info.ToString() + " {" + Environment.NewLine;
            foreach (var item in (IEnumerable)info) {
               processed += tabs + tab + ProcessObject(item, tabLevel + 1);
            }
            processed += tabs + "}" + Environment.NewLine;
         } else {
            processed = tabs + info.ToString() + Environment.NewLine;
         }
         return processed;
      }
   }

   public class SnoopHelper {
      /// <summary>
      ///  Given a root element, returns a list of all commands available within that element.
      /// </summary>
      public IList<ICommand> SnoopCommands(FrameworkElement element) {
         if (element == null) return null;
         var set = new HashSet<ICommand>();
         foreach (var binding in element.CommandBindings) set.Add(((CommandBinding)binding).Command);
         if (element is Panel) {
            var panel = (Panel)element;
            foreach (var child in panel.Children) {
               if (child is FrameworkElement) foreach (var command in SnoopCommands((FrameworkElement)child)) set.Add(command);
            }
         } else if (element is Decorator) {
            var child = ((Decorator)element).Child as FrameworkElement;
            if (child != null) foreach (var command in SnoopCommands(child)) set.Add(command);
         } else if (element is ContentControl) {
            var child = ((ContentControl)element).Content as FrameworkElement;
            if (child != null) foreach (var command in SnoopCommands(child)) set.Add(command);
         }
         return set.ToList();
      }

      /// <summary>
      /// Wraps the object in an object that acts exactly like the original, except with all fields, properties, and methods public.
      /// Calling a Field or Property on that wrapped object returns a Cheater wrapper around the result, making all its members public as well.
      /// </summary>
      public Cheater Wrap(object source) {
         return new Cheater(source);
      }

      public object Unwrap(Cheater cheater) {
         return cheater._source;
      }
   }

   /// <summary>
   /// Provides a wrapper over an object that acts exactly like that object, except with all fields, properties, and methods public.
   /// Calling a Field or Property returns a Cheater wrapper around that result, making all of its members public as well.
   /// </summary>
   public class Cheater : DynamicObject, IEnumerable<string> {
      internal object _source;
      private IList<string> _valueMembers = new List<string>();

      static BindingFlags allInstance = BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance;

      public Cheater(object source) {
         _source = source;

         FillMembers(_source.GetType(), interfaces: true);
      }

      public void FillMembers(Type type, bool interfaces) {
         var members = _source.GetType().GetMembers(allInstance);
         foreach (var member in members) {
            if (member.DeclaringType != type && !member.DeclaringType.IsInterface) continue;
            if (member.DeclaringType.IsInterface && !interfaces) continue;
            var field = member as FieldInfo;
            var property = member as PropertyInfo;
            if (field != null) {
               _valueMembers.Add(member.Name + " : " + field.GetValue(_source));
            }
            if (property != null) {
               var getter = property.GetGetMethod(nonPublic: true);
               if (getter != null && getter.GetParameters().Length == 0) _valueMembers.Add(member.Name + " : " + getter.Invoke(_source, new object[0]));
            }
         }

         if (type != typeof(object)) {
            _valueMembers.Add(Environment.NewLine + "------------- " + type.Name);
            FillMembers(type.BaseType, interfaces: false);
         }
      }

      public override string ToString() {
         return _source.ToString();
      }

      public override bool TryGetMember(GetMemberBinder binder, out object result) {
         var field = _source.GetType().GetField(binder.Name, allInstance);
         if (field != null) {
            result = new Cheater(field.GetValue(_source));
            return true;
         }

         var property = _source.GetType().GetProperty(binder.Name, allInstance);
         if (property != null) {
            var getter = property.GetGetMethod(nonPublic: true);
            result = null;
            if (getter != null) {
               result = new Cheater(getter.Invoke(_source, new object[0]));
            }
            return getter != null;
         }

         return base.TryGetMember(binder, out result);
      }

      public override bool TrySetMember(SetMemberBinder binder, object value) {
         var field = _source.GetType().GetField(binder.Name, allInstance);
         if (field != null) {
            field.SetValue(_source, value);
            return true;
         }

         var property = _source.GetType().GetProperty(binder.Name, allInstance);
         if (property != null) {
            var setter = property.GetSetMethod(nonPublic: true);
            if (setter != null) {
               setter.Invoke(_source, new object[] { value });
            }
            return setter != null;
         }

         return base.TrySetMember(binder, value);
      }

      #region IEnumerable

      public IEnumerator<string> GetEnumerator() {
         return _valueMembers.GetEnumerator();
      }

      IEnumerator IEnumerable.GetEnumerator() {
         return this.GetEnumerator();
      }

      #endregion
   }
}
