using System;

namespace EchoDotNetLite.Common
{
    internal interface INotifyCollectionChanged<T>
    {
        event EventHandler<(CollectionChangeType, T)> OnCollectionChanged;
        void RaiseCollectionChanged(CollectionChangeType type, T item);
    }
}
