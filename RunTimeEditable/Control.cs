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
using System.Windows.Input;
using System.Windows.Markup;

namespace RuntimeEditable {
   public interface IControlScope {
      void OpenScope(ElementTabSet tabset);
      void CloseScope();
      bool ObserveChange(string file);
      IEnumerable<ScriptScope> Scopes();
   }

   /// <summary>
   /// 
   /// When you're about to do parsing for the window, open a scope.
   /// Any controls parsed as subcontrols of the window are added to the scope's control list.
   /// 
   /// When a xaml file other than the window changes, inform the scope.
   /// It will determine if anything needs to be updated.
   /// </summary>
   public class Control : Decorator {
      #region ControlScope

      static ControlScope _currentCreationScope;
      static int _currentScopeDepth;

      class ControlScope : List<Control>, IControlScope {
         public readonly string Directory;
         public readonly ScriptEngine Engine;
         int _scopeDepth;
         ControlScope _prevScope;

         public ElementTabSet ElementTabSet { get; private set; }

         public ControlScope(string directory, ScriptEngine engine) { Directory = directory; Engine = engine; }

         public void OpenScope(ElementTabSet tabset) {
            foreach (var scope in Scopes().ToList()) tabset.RemoveTab(scope);
            Clear();
            _prevScope = _currentCreationScope;
            _currentScopeDepth++;
            if (_currentScopeDepth > 20) throw new StackOverflowException("Cannot controls within controls 20 levels deep. Do you have control recursion?");
            _scopeDepth = _currentScopeDepth;
            ElementTabSet = tabset;
            _currentCreationScope = this;
         }

         public void CloseScope() {
            Debug.Assert(_scopeDepth == _currentScopeDepth);
            _currentScopeDepth--;
            _currentCreationScope = _prevScope;
            ElementTabSet = null;
         }

         public bool ObserveChange(string file) {
            bool result = false;
            foreach (var control in this) result = control.ObserveChange(file) || result;
            return result;
         }

         public IEnumerable<ScriptScope> Scopes() {
            foreach (var control in this) {
               foreach (var scope in control._elementTab.Scopes()) {
                  yield return scope;
               }
            }
            // return this.Select(control => control.Scopes()).Aggregate(Enumerable.Concat);
         }
      }

      public static IControlScope CreateScope(string directory, ScriptEngine engine) { return new ControlScope(directory, engine); }

      #endregion

      #region Source

      public string Source {
         get { return _elementTab.Source; }
         set {
            _elementTab.Source = value;
            Child = _elementTab.Element;
         }
      }

      #endregion

      readonly ElementTab _elementTab;
      readonly ControlScope _parentScope;

      public Control() {
         _parentScope = _currentCreationScope;
         _elementTab = new ElementTab(_parentScope.Engine.CreateScope(), _parentScope.Directory, null, _parentScope.ElementTabSet, false);
         _parentScope.ElementTabSet.AddTab(_elementTab);
         _parentScope.Add(this);
      }

      bool ObserveChange(string file) {
         if (_elementTab.Refresh(file)) {
            Child = _elementTab.Element;
            return true;
         }

         return false;
      }
   }
}
