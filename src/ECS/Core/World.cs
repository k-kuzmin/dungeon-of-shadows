namespace DungeonOfShadows.ECS;

public class World
{
    private int _nextId;
    private readonly HashSet<int> _alive = new();
    private readonly Dictionary<Type, IComponentStore> _stores = new();

    public int CreateEntity()
    {
        int id = _nextId++;
        _alive.Add(id);
        return id;
    }

    public void DestroyEntity(int id)
    {
        _alive.Remove(id);
        foreach (var store in _stores.Values)
            store.Remove(id);
    }

    public bool IsAlive(int id) => _alive.Contains(id);

    public ComponentStore<T> Store<T>() where T : struct
    {
        // Fast path: static generic cache (no dictionary lookup)
        if (StoreCache<T>.Instance != null)
            return StoreCache<T>.Instance;

        if (!_stores.TryGetValue(typeof(T), out var store))
        {
            store = new ComponentStore<T>();
            _stores[typeof(T)] = store;
        }

        var typed = (ComponentStore<T>)store;
        StoreCache<T>.Instance = typed;
        return typed;
    }

    public void Add<T>(int id, T component) where T : struct =>
        Store<T>().Add(id, component);

    public ref T Get<T>(int id) where T : struct =>
        ref Store<T>().Get(id);

    public bool Has<T>(int id) where T : struct =>
        Store<T>().Has(id);

    public void Remove<T>(int id) where T : struct =>
        Store<T>().Remove(id);

    /// <summary>
    /// Zero-allocation query: fills a reusable buffer with matching entity IDs.
    /// </summary>
    public void QueryInto<T>(List<int> results) where T : struct
    {
        results.Clear();
        foreach (int id in Store<T>().Entities)
            if (_alive.Contains(id))
                results.Add(id);
    }

    /// <summary>
    /// Zero-allocation query with two component filters.
    /// </summary>
    public void QueryInto<T1, T2>(List<int> results) where T1 : struct where T2 : struct
    {
        results.Clear();
        var store1 = Store<T1>();
        var store2 = Store<T2>();

        // Iterate over the smaller store
        if (store1.Count <= store2.Count)
        {
            foreach (int id in store1.Entities)
                if (_alive.Contains(id) && store2.Has(id))
                    results.Add(id);
        }
        else
        {
            foreach (int id in store2.Entities)
                if (_alive.Contains(id) && store1.Has(id))
                    results.Add(id);
        }
    }

    public IEnumerable<int> AllEntities => _alive;

    // Static generic cache — avoids Dictionary<Type> lookup on hot path
    private static class StoreCache<T> where T : struct
    {
        internal static ComponentStore<T>? Instance;
    }
}
