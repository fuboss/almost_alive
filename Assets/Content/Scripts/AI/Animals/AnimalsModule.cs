using System.Collections.Generic;
using Content.Scripts.AI.GOAP;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Core.Simulation;
using Content.Scripts.Game;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.AI.Animals {
  /// <summary>
  /// Central manager for all animal herds.
  /// Handles spawning, registration, and provides queries for herd members.
  /// </summary>
  public class AnimalsModule : IStartable {
    [Inject] private SimulationLoop _simLoop;
    [Inject] private IObjectResolver _resolver;
    [Inject] private ActorCreationModule _actorCreation;

    private readonly List<HerdAnchor> _anchors = new();
    private readonly Dictionary<HerdAnchor, List<IGoapAnimalAgent>> _herds = new();
    private readonly Dictionary<IGoapAnimalAgent, HerdAnchor> _animalToAnchor = new();

    private float _worldRadius = 100f;

    public IReadOnlyList<HerdAnchor> anchors => _anchors;

    public void Start() {
      // Initial setup if needed
    }

    public void SetWorldRadius(float radius) {
      _worldRadius = radius;
      foreach (var anchor in _anchors) {
        anchor.SetWorldRadius(radius);
      }
    }

    public HerdAnchor CreateHerd(Vector3 position) {
      var go = new GameObject($"HerdAnchor_{_anchors.Count}");
      go.transform.position = position;

      var anchor = go.AddComponent<HerdAnchor>();
      anchor.SetWorldRadius(_worldRadius);
      _simLoop.Register(anchor);

      _anchors.Add(anchor);
      _herds[anchor] = new List<IGoapAnimalAgent>();
      _resolver.Inject(anchor);
      Debug.Log($"[AnimalsModule] Created herd at {position}");
      return anchor;
    }

    public void DespawnHerd(HerdAnchor anchor) {
      if (anchor == null || !_herds.TryGetValue(anchor, out var members)) return;

      // Despawn all members first
      foreach (var animal in members.ToArray()) {
        DespawnAnimal(animal);
      }

      _simLoop.Unregister(anchor);
      _herds.Remove(anchor);
      _anchors.Remove(anchor);
      Object.Destroy(anchor.gameObject);

      Debug.Log("[AnimalsModule] Despawned herd");
    }

    // ═══════════════════════════════════════════════════════════════
    // ANIMAL MANAGEMENT
    // ═══════════════════════════════════════════════════════════════

    public void AddToHerd(HerdAnchor anchor, IGoapAnimalAgent animal) {
      if (anchor == null || animal == null) return;

      // Remove from previous herd
      RemoveFromHerd(animal);

      if (!_herds.ContainsKey(anchor)) {
        _herds[anchor] = new List<IGoapAnimalAgent>();
      }

      _herds[anchor].Add(animal);
      _animalToAnchor[animal] = anchor;
      animal.herdId = anchor.GetInstanceID();
    }

    public void RemoveFromHerd(IGoapAnimalAgent animal) {
      if (animal == null) return;
      if (!_animalToAnchor.TryGetValue(animal, out var anchor)) return;

      _herds[anchor]?.Remove(animal);
      _animalToAnchor.Remove(animal);
      animal.herdId = -1;
    }

    public void DespawnAnimal(IGoapAnimalAgent animal) {
      if (animal == null) return;
      RemoveFromHerd(animal);
      Object.Destroy(animal.gameObject);
    }

    // ═══════════════════════════════════════════════════════════════
    // QUERIES
    // ═══════════════════════════════════════════════════════════════

    public IReadOnlyList<IGoapAnimalAgent> GetHerdMembers(HerdAnchor anchor) {
      if (anchor == null || !_herds.TryGetValue(anchor, out var members)) {
        return System.Array.Empty<IGoapAnimalAgent>();
      }

      return members;
    }

    public HerdAnchor GetAnchor(IGoapAnimalAgent animal) {
      if (animal == null) return null;
      return _animalToAnchor.TryGetValue(animal, out var anchor) ? anchor : null;
    }

    public HerdAnchor GetNearestHerd(Vector3 position) {
      HerdAnchor nearest = null;
      var minDist = float.MaxValue;

      foreach (var anchor in _anchors) {
        var dist = Vector3.Distance(position, anchor.position);
        if (dist < minDist) {
          minDist = dist;
          nearest = anchor;
        }
      }

      return nearest;
    }

    public int GetHerdSize(HerdAnchor anchor) {
      return _herds.TryGetValue(anchor, out var members) ? members.Count : 0;
    }

    public HerdAnchor SpawnHerd(string animalActorKey, Vector3 position, int count, float radius = 5f) {
      if (string.IsNullOrWhiteSpace(animalActorKey)) {
        Debug.LogError("[AnimalsModule] animalActorKey is invalid");
        return null;
      }

      var anchor = CreateHerd(position);

      for (int i = 0; i < count; i++) {
        var offset = Random.insideUnitSphere * radius;
        offset.y = 0;
        var spawnPos = position + offset;
        if (!_actorCreation.TrySpawnActor(animalActorKey, spawnPos, out var animalDescriptor)) continue;

        animalDescriptor.transform.rotation = Quaternion.Euler(0, Random.Range(0, 360f), 0);
        var animal = animalDescriptor.GetComponent<IGoapAnimalAgent>();
        animal.OnCreated();
        AddToHerd(anchor, animal);

        // Initialize HerdingBehavior
        var herding = animal.herdingBehavior;
        if (herding != null) {
          herding.Initialize(anchor);
        }
      }

      Debug.Log($"[AnimalsModule] Spawned herd: {count} animals at {position}");
      return anchor;
    }
  }
}