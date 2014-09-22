using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Dynamic;

namespace RuntimeEditable {
   class ExpandoDependencyObject : DynamicObject, IDictionary<string, object>, INotifyCollectionChanged, INotifyPropertyChanged {
      readonly IDictionary<string, object> _members = new Dictionary<string, object>();

      public event PropertyChangedEventHandler PropertyChanged;
      public event NotifyCollectionChangedEventHandler CollectionChanged;

      public override bool TrySetMember(SetMemberBinder binder, object value) {
         this[binder.Name] = value;
         return true;
      }

      public override bool TryGetMember(GetMemberBinder binder, out object result) {
         result = _members.ContainsKey(binder.Name) ? _members[binder.Name] : null;
         return result != null;
      }

      void Notify(string prop) {
         if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(prop));
      }
      void Notify(NotifyCollectionChangedAction action) {
         if (CollectionChanged != null) CollectionChanged(this, new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
      }

      #region IDictionary

      public void Add(string key, object value) {
         _members.Add(key, value);
         Notify(NotifyCollectionChangedAction.Add);
      }
      public bool ContainsKey(string key) { return _members.ContainsKey(key); }
      public ICollection<string> Keys { get { return _members.Keys; } }
      public bool Remove(string key) {
         var result = _members.Remove(key);
         Notify(NotifyCollectionChangedAction.Remove);
         return result;
      }
      public bool TryGetValue(string key, out object value) { return _members.TryGetValue(key, out value); }
      public ICollection<object> Values { get { return _members.Values; } }
      public object this[string key] {
         get { return _members[key]; }
         set {
            bool isNew = !_members.ContainsKey(key);
            _members[key] = value;
            if (isNew) {
               Notify(NotifyCollectionChangedAction.Add);
            } else {
               Notify(NotifyCollectionChangedAction.Replace);
               Notify(key);
            }
         }
      }
      public void Add(KeyValuePair<string, object> item) { _members.Add(item); Notify(NotifyCollectionChangedAction.Add); }
      public void Clear() { _members.Clear(); Notify(NotifyCollectionChangedAction.Reset); }
      public bool Contains(KeyValuePair<string, object> item) { return _members.Contains(item); }
      public void CopyTo(KeyValuePair<string, object>[] array, int arrayIndex) { _members.CopyTo(array, arrayIndex); }
      public int Count { get { return _members.Count; } }
      public bool IsReadOnly { get { return _members.IsReadOnly; } }
      public bool Remove(KeyValuePair<string, object> item) {
         var result = _members.Remove(item);
         Notify(NotifyCollectionChangedAction.Remove);
         return result;
      }
      public IEnumerator<KeyValuePair<string, object>> GetEnumerator() { return _members.GetEnumerator(); }
      System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return _members.GetEnumerator(); }

      #endregion
   }
}
