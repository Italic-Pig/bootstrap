using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ItalicPig.Bootstrap
{
    /// <summary>Extended version of ObservableCollection with support for range operations.</summary>
    public class ObservableCollection<T> : System.Collections.ObjectModel.ObservableCollection<T>
    {
        public ObservableCollection() { }
        public ObservableCollection(IEnumerable<T> collection) : base(collection) { }
        public ObservableCollection(List<T> list) : base(list) { }

        /// <summary>Adds the elements of the specified collection to the end of the ObservableCollection<T>.</summary>
        public void AddRange([NotNull] IEnumerable<T> collection)
        {
            foreach (var Item in collection)
            {
                Items.Add(Item);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, collection.ToList()));
        }

        /// <summary>Removes the first occurence of each item in the specified collection from ObservableCollection<T>.</summary>
        public void RemoveRange([NotNull] IEnumerable<T> collection)
        {
            foreach (var Item in collection)
            {
                Items.Remove(Item);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, collection.ToList()));
        }

        /// <summary>Clears the current collection and replaces it with the specified item.</summary>
        public void Reset(T item) => ResetRange(new T[] { item });

        /// <summary>Clears the current collection and replaces it with the specified collection.</summary>
        public void ResetRange([NotNull] IEnumerable<T> collection)
        {
            Items.Clear();
            foreach (var Item in collection)
            {
                Items.Add(Item);
            }

            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
