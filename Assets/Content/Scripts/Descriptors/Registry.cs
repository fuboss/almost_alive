using System;
using System.Collections.Generic;
using UnityEngine;

namespace Content.Scripts.Game {
  public static class Registry<T>
    where T : class, new() {
    public static event Action<T> OmAdded = delegate { };
    public static event Action<T> OnRemoved = delegate { };

    private static readonly Dictionary<int, T> items = new();
    private static readonly Dictionary<T, int> itemsIds = new();
    private static int _id = 0;

    public static int Register(T item) {
      if (item == null || !items.TryAdd(++_id, item)) return -1;
      itemsIds[item] = _id;
      return _id;
    }

    public static void Unregister(T item) {
      if (!itemsIds.TryGetValue(item, out var id)) return;
      items.Remove(id);
      itemsIds.Remove(item);
    }

    public static bool TryGet(int id, out T item) {
      return items.TryGetValue(id, out item);
    }

    public static T GetById(int id) {
      items.TryGetValue(id, out var item);
      return item;
    }

    public static IReadOnlyCollection<T> GetAll() {
      return items.Values;
    }

    public static int count => items.Count;

    [RuntimeInitializeOnLoadMethod]
    public static void Clear() {
      items.Clear();
      itemsIds.Clear();
      _id = 0;
    }
  }
}