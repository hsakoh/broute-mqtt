using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace EchoDotNetLite.Common
{
    internal class NotifyChangeCollection<TParent, TItem> : ICollection<TItem> where TParent : INotifyCollectionChanged<TItem>
    {
        public NotifyChangeCollection(TParent parentNode)
        {
            ParentNode = parentNode;
            InnnerConnection = new List<TItem>();
        }
        private TParent ParentNode;
        private List<TItem> InnnerConnection;
        public int Count => InnnerConnection.Count;

        public bool IsReadOnly => false;

        public void Add(TItem item)
        {
            InnnerConnection.Add(item);
            ParentNode.RaiseCollectionChanged(CollectionChangeType.Add,item);
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
                ParentNode.RaiseCollectionChanged(CollectionChangeType.Remove, item);
            }
        }

        public IEnumerator<TItem> GetEnumerator()
        {
            return InnnerConnection.GetEnumerator();
        }

        public bool Remove(TItem item)
        {
            ParentNode.RaiseCollectionChanged(CollectionChangeType.Remove, item);
            return InnnerConnection.Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return InnnerConnection.GetEnumerator();
        }
    }
}
