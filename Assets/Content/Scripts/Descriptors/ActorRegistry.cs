using System.Collections.Generic;
using Content.Scripts.Core;
using UnityEngine;

namespace Content.Scripts.Game {
  /// <summary>
  /// Generic registry for Components attached to actors with ActorId.
  /// Auto-indexed by ActorId.id for fast lookup.
  /// </summary>
  public static class ActorRegistry<T> where T : Component {
    private static readonly Dictionary<int, T> _byActorId = new();
    private static readonly HashSet<T> _all = new();

    static ActorRegistry() {
      StaticResetRegistry.RegisterReset(Clear);
    }

    public static void Register(T component) {
      if (component == null) return;

      var actorId = component.GetComponent<ActorId>();
      if (actorId != null && actorId.id >= 0) {
        _byActorId[actorId.id] = component;
      }
      _all.Add(component);
    }

    public static void Unregister(T component) {
      if (component == null) return;

      var actorId = component.GetComponent<ActorId>();
      if (actorId != null) {
        _byActorId.Remove(actorId.id);
      }
      _all.Remove(component);
    }

    public static T GetByActorId(int id) => _byActorId.GetValueOrDefault(id);
    public static IReadOnlyCollection<T> all => _all;
    public static int count => _all.Count;
    public static bool Contains(T component) => _all.Contains(component);
    public static bool TryGet(int actorId, out T component) => _byActorId.TryGetValue(actorId, out component);

    public static void Clear() {
      _byActorId.Clear();
      _all.Clear();
    }
  }
}
