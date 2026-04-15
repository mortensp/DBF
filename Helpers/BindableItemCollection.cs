using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Caliburn.Micro;
using System.Collections.Specialized;

namespace DBF.Helper;

public class BindableItemCollection<T> : BindableCollection<T>
    where T : class
{
    public event EventHandler<ItemPropertyChangedEventArgs<T>> ItemChanged;

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        // Unhook removed items
        if (e.OldItems != null)
            foreach (var oldItem in e.OldItems)
                UnhookItem(oldItem);

        // Hook added items
        if (e.NewItems != null)
            foreach (var newItem in e.NewItems)
                HookItem(newItem);

        base.OnCollectionChanged(e);
    }

    private void HookItem(object item)
    {
        if (item is INotifyPropertyChanged npc)
            npc.PropertyChanged += Item_PropertyChanged;
    }

    private void UnhookItem(object item)
    {
        if (item is INotifyPropertyChanged npc)
            npc.PropertyChanged -= Item_PropertyChanged;
    }

    private void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        // Raise strongly typed event
        if (sender is T typed)
            ItemChanged?.Invoke(this, new ItemPropertyChangedEventArgs<T>(typed, e.PropertyName));

        // Notify UI that an item changed
        NotifyOfPropertyChange("Item[]");
    }
}

public class ItemPropertyChangedEventArgs<T> : EventArgs
{
    public T      Item         { get; }
    public string PropertyName { get; }

    public ItemPropertyChangedEventArgs(T item, string propertyName)
    {
        Item         = item;
        PropertyName = propertyName;
    }
}
