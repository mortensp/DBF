using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using Caliburn.Micro;

namespace DBF.Helpers;

// tilføjet af mspa
/// <summary>
/// Represents an observable collection of objects that notifies listeners of changes to its items and also
/// propagates property change notifications from its contained elements.
/// </summary>
/// <remarks>In addition to standard collection change notifications, this collection automatically
/// subscribes to the <see cref="INotifyPropertyChanged.PropertyChanged"/> event of each item. When an item's
/// property changes, the collection raises a corresponding collection change event, allowing consumers to react to
/// changes within individual items. This is useful for scenarios where both collection-level and item-level changes
/// need to be observed, such as in data binding contexts.</remarks>
/// <typeparam name="T">The type of elements in the collection. Must implement <see cref="INotifyPropertyChanged"/> to support property
/// change notification propagation.</typeparam>
public class BindableCollectionExt<T> : BindableCollection<T>, INotifyPropertyChanged where T : INotifyPropertyChanged
{
    #region Public Constructors
        /// <summary>
        /// Initializes a new instance of the BindableCollectionExt class.
        /// </summary>
        public BindableCollectionExt()
        {
            CollectionChanged+= collectionChanged;
        }

        /// <summary>
        /// Initializes a new instance of the BindableCollectionExt class that contains elements copied from the
        /// specified list.
        /// </summary>
        /// <remarks>
        /// Unlike the base &lt;T&gt; constructor, this constructor copies the elements from
        /// the provided list rather than using the list directly as the internal storage. This ensures that subsequent
        /// modifications to the original list do not affect the collection.
        /// </remarks>
        /// <param name="list">The list whose elements are copied to the new collection. If null, the collection is initialized as empty.</param>
        public BindableCollectionExt(List<T> list) : base((list != null) ? new List<T>(list.Count) : list)
        {
            // Workaround for VSWhidbey bug 562681 (tracked by Windows bug 1369339).
            // We should be able to simply call the base(list) ctor.  But Collection<T>
            // doesn't copy the list (contrary to the documentation) - it uses the
            // list directly as its storage.  So we do the copying here.
            //
            CopyFrom(list);
        }

        /// <summary>
        /// Initializes a new instance of the ObservableCollection class that contains
        /// elements copied from the specified collection and has sufficient capacity
        /// to accommodate the number of elements copied.
        /// </summary>
        /// <param name="collection">The collection whose elements are copied to the new list.</param>
        /// <remarks>
        /// The elements are copied onto the ObservableCollection in the
        /// same order they are read by the enumerator of the collection.
        /// </remarks>
        /// <exception cref="ArgumentNullException"> collection is a null reference </exception>
        public BindableCollectionExt(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException(nameof(collection));

            CopyFrom(collection);
        }
    #endregion Public Constructors

    #region Public Methods
        /// <summary>
        ///     Refreshes collection by fireing a NotifyCollectionChangedAction.Reset event
        /// </summary>
        public new void Refresh()
        {
            base.Refresh();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    #endregion

    #region Private Methods
        private void collectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            //if (IsNotifying)
            //Tracer.PropertyChange($"{e.Action}()", GetInvocationList());
            if (e.Action == NotifyCollectionChangedAction.Remove
            ||  e.Action == NotifyCollectionChangedAction.Replace)
                if (e.OldItems != null)
                    foreach (INotifyPropertyChanged item in e.OldItems)
                        item.PropertyChanged -= item_PropertyChanged;

            if (e.Action == NotifyCollectionChangedAction.Add
            ||  e.Action == NotifyCollectionChangedAction.Replace)
                if (e.NewItems != null)
                    foreach (INotifyPropertyChanged item in e.NewItems)
                        item.PropertyChanged += item_PropertyChanged;
        }

        private void CopyFrom(IEnumerable<T> collection)
        {
            IList<T> items = Items;

            if (collection != null && items != null)
            {
                using IEnumerator<T> enumerator = collection.GetEnumerator();

                while (enumerator.MoveNext())
                    items.Add(enumerator.Current);
            }
        }

        private void item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            var item  = (T)sender;
            var index = IndexOf(item);

            if (index >= 0)
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item, item, index));
            else
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        ///// <summary>
        ///// Delegate[] list of methods to be call on Event
        ///// </summary>        
        ///// <returns>List of Methods to be called On Collection Changed</returns>
        //public Delegate[] GetInvocationList()
        //{
        //    Type classType = GetType();

        //    FieldInfo eventField = classType.BaseType.GetRuntimeFields().FirstOrDefault(f => f.Name == nameof(CollectionChanged));
        //    var       _delegate  = (Delegate)eventField?.GetValue(this);

        //    return _delegate?.GetInvocationList();
        //}
    #endregion Private Methods
}
