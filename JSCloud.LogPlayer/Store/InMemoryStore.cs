using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JSCloud.LogPlayer.Types;

namespace JSCloud.LogPlayer.Store
{
    public class InMemoryStore<I> : IStore<I>
        where I : struct, IComparable<I>
    {

        private readonly IStore<I> _baseStore;
        private readonly ConcurrentDictionary<string, ConcurrentBag<ChangeLog<I>>> _items;

        public InMemoryStore(IStore<I> BaseStore)
        {
            _baseStore = BaseStore;
            _items = new ConcurrentDictionary<string, ConcurrentBag<ChangeLog<I>>>();
        }

        public async Task<ICollection<ChangeLog<I>>> GetChangesAsync(I? objectId, string fullTypeName)
        {
            if (_items.ContainsKey(fullTypeName))
            {
                var items = _items[fullTypeName];
                return await Task.Run(() =>
                {
                    return items.Where(x => (x.ObjectId.Equals(objectId) || !objectId.HasValue)
                                  && x.FullTypeName == fullTypeName).ToList();
                });
            }
            return new LinkedList<ChangeLog<I>>();
        }

        public async Task Provision()
        {
            if (_baseStore != null)
            {
                await _baseStore.Provision();
                var allItems = await _baseStore.GetChangesAsync(null, null);
                allItems.AsParallel().ForAll(x =>
                {
                    if (!_items.ContainsKey(x.FullTypeName))
                    {
                        _items.TryAdd(x.FullTypeName, new ConcurrentBag<ChangeLog<I>>());
                    }
                    _items[x.FullTypeName].Add(x);
                });
            }
        }

        public async Task<ChangeLog<I>> StoreAsync(ChangeLog<I> changeLog)
        {
            if (_baseStore != null)
            {
                changeLog = await _baseStore.StoreAsync(changeLog);
            }
            else
            {
                changeLog.ChangeLogId = Guid.NewGuid();
            }
            if (!_items.ContainsKey(changeLog.FullTypeName))
            {
                _items.TryAdd(changeLog.FullTypeName, new ConcurrentBag<ChangeLog<I>>());
            }

            _items[changeLog.FullTypeName].Add(changeLog);
            return changeLog;
        }

        public async Task<ICollection<ChangeLog<I>>> StoreAsync(ICollection<ChangeLog<I>> changeLogs)
        {
            if (changeLogs.Count == 0)
            {
                return changeLogs;
            }

            if (_baseStore != null)
            {
                changeLogs = await _baseStore.StoreAsync(changeLogs);
            }
            else
            {
                for(int i = 0; i < changeLogs.Count; i ++)
                {
                    changeLogs.ElementAt(i).ChangeLogId = Guid.NewGuid();
                }
            }
            if (!_items.ContainsKey(changeLogs.ElementAt(0).FullTypeName))
            {
                _items.TryAdd(changeLogs.ElementAt(0).FullTypeName, new ConcurrentBag<ChangeLog<I>>());
            }
            changeLogs.AsParallel().ForAll(x => _items[x.FullTypeName].Add(x));
            return changeLogs;
        }
    }
}
