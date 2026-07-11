using Caliburn.Micro;

namespace DBF.Helpers;

// Extension method
public static class CollectionExtensions
{
    public static void ReplaceRange<T>(this BindableCollection<T> collection, IEnumerable<T> items)
    {
        var wasNotifying       =collection.IsNotifying;
        collection.IsNotifying = false;

        collection.Clear();

        foreach (var item in items)
            collection.Add(item);

        collection.IsNotifying = wasNotifying;

    }
}
