using System;
using System.Collections.Generic;

namespace Content.Scripts.Game {
  public static class Registry<T>
    where T : class, new() {
    public static event Action<T> OmAdded = delegate { };
    public static event Action<T> OnRemoved = delegate { };

    private static readonly HashSet<T> items = new HashSet<T>();
    private static int _id = 0;

    public static int Register(T item) {
      if (item == null || !items.Add(item)) return -1;
      return _id++;
    }

    public static void Unregister(T item) {
      if (items.Remove(item)) {
      }
    }

    public static IEnumerable<T> GetAll() {
      return items;
    }

    public static int count => items.Count;

    public static void Clear() {
      items.Clear();
      _id = 0;
    }
  }
}