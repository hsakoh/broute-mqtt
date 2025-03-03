using System.Collections;
using System.Collections.Generic;

namespace EchoDotNetLite.Common
{
    internal class NotifyChangeCollection<TParent, TItem>(TParent parentNode) : ICollection<TItem> where TParent : INotifyCollectionChanged<TItem>
    {
        private readonly List<TItem> InnnerConnection = [];
        public int Count => InnnerConnection.Count;

        public bool IsReadOnly => false;

        public void Add(TItem item)
        {
            InnnerConnection.Add(item);
            parentNode.RaiseCollectionChanged(CollectionChangeType.Add, item);
        }

        public void Clear()
        {
            //var temp = InnnerConnection.ToArray();
            InnnerConnection.Clear();
            //foreach (var item in temp)
            //{
            //    ParentNode.RaiseCollectionChanged(CollectionChangeType.Remove, item);
            //}
        }

        public bool Contains(TItem item)
        {
            return InnnerConnection.Contains(item);
        }

        public void CopyTo(TItem[] array, int arrayIndex)
        {
            InnnerConnection.CopyTo(array, arrayIndex);
            foreach (var item in array)
            {
                parentNode.RaiseCollectionChanged(CollectionChangeType.Remove, item);
            }
        }

        public IEnumerator<TItem> GetEnumerator()
        {
            return InnnerConnection.GetEnumerator();
        }

        public bool Remove(TItem item)
        {
            parentNode.RaiseCollectionChanged(CollectionChangeType.Remove, item);
            return InnnerConnection.Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return InnnerConnection.GetEnumerator();
        }
    }
}
