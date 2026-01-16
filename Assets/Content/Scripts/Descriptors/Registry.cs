using System;
using System.Collections.Generic;
using Content.Scripts.Core;

namespace Content.Scripts.Game {
  public static class Registry<T> where T : class, new() {
    public static event Action<T> OnAdded = delegate { };
    public static event Action<T> OnRemoved = delegate { };

    private static readonly Dictionary<int, T> _items = new();
    private static readonly Dictionary<T, int> _itemsIds = new();
    private static int _idCounter;

    static Registry() {
      StaticResetRegistry.RegisterReset(Clear);
    }

    public static int Register(T item) {
      if (item == null || !_items.TryAdd(++_idCounter, item)) return -1;
      _itemsIds[item] = _idCounter;
      OnAdded.Invoke(item);
      return _idCounter;
    }

    public static void Unregister(T item) {
      if (!_itemsIds.TryGetValue(item, out var id)) return;
      _items.Remove(id);
      _itemsIds.Remove(item);
      OnRemoved.Invoke(item);
    }

    public static bool TryGet(int id, out T item) => _items.TryGetValue(id, out item);
    public static T GetById(int id) => _items.GetValueOrDefault(id);
    public static IReadOnlyCollection<T> GetAll() => _items.Values;
    public static int count => _items.Count;

    public static void Clear() {
      _items.Clear();
      _itemsIds.Clear();
      _idCounter = 0;
      OnAdded = delegate { };
      OnRemoved = delegate { };
    }
  }
}
