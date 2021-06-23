using System.Collections;
using System.Collections.Generic;
using ServiceComponents.AspNetCore.Hosting.Items;

namespace ServiceComponents.AspNetCore.Hosting
{
    public class RequestPipe : ICollection<PipeItem>
    {
        private ICollection<PipeItem> _items = new List<PipeItem>();

        public int Count => _items.Count;
        public bool IsReadOnly => _items.IsReadOnly;

        public void Add(PipeItem item)
        {
            _items.Add(item);
        }

        

        public void Clear()
        {
            _items.Clear();
        }

        public bool Contains(PipeItem item)
        {
            return _items.Contains(item);
        }

        public void CopyTo(PipeItem[] array, int arrayIndex)
        {
            _items.CopyTo(array, arrayIndex);
        }

        public IEnumerator<PipeItem> GetEnumerator()
        {
            return _items.GetEnumerator();
        }

        public bool Remove(PipeItem item)
        {
            return _items.Remove(item);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return _items.GetEnumerator();
        }
    }
}
