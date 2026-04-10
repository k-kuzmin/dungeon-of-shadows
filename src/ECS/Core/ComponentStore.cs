using System.Runtime.InteropServices;

namespace DungeonOfShadows.ECS;

public interface IComponentStore
{
    bool Has(int id);
    void Remove(int id);
}

public class ComponentStore<T> : IComponentStore where T : struct
{
    private readonly Dictionary<int, T> _data = new();

    public void Add(int id, T component) => _data[id] = component;

    public ref T Get(int id) =>
        ref CollectionsMarshal.GetValueRefOrNullRef(_data, id);

    public bool Has(int id) => _data.ContainsKey(id);

    public void Remove(int id) => _data.Remove(id);

    // Returns concrete KeyCollection — zero-allocation foreach
    public Dictionary<int, T>.KeyCollection Entities => _data.Keys;

    public int Count => _data.Count;
}
