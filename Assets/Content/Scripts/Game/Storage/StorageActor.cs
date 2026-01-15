using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Game.Storage {
  /// <summary>
  /// Storage actor - container that accepts specific item types.
  /// Requires: ActorDescription, FilteredActorInventory, ActorPriority
  /// </summary>
  [RequireComponent(typeof(ActorDescription))]
  [RequireComponent(typeof(FilteredActorInventory))]
  [RequireComponent(typeof(ActorPriority))]
  public class StorageActor : MonoBehaviour {
    [ShowInInspector, ReadOnly] private FilteredActorInventory _inventory;
    [ShowInInspector, ReadOnly] private ActorPriority _priority;
    [ShowInInspector, ReadOnly] private ActorDescription _description;

    public FilteredActorInventory inventory => _inventory;
    public ActorPriority priority => _priority;
    public ActorDescription description => _description;
    
    public string[] acceptedTags => _inventory.availableTags;
    public int totalSlots => _inventory.slotCount;
    public int freeSlots => _inventory.freelots.Count();
    public int occupiedSlots => _inventory.occupiedSlots.Count();
    public bool isFull => freeSlots == 0;
    public bool isEmpty => occupiedSlots == 0;
    public float fillRatio => totalSlots > 0 ? (float)occupiedSlots / totalSlots : 0f;

    private void Awake() {
      _inventory = GetComponent<FilteredActorInventory>();
      _priority = GetComponent<ActorPriority>();
      _description = GetComponent<ActorDescription>();
    }

    private void OnEnable() {
      ActorRegistry<StorageActor>.Register(this);
    }

    private void OnDisable() {
      ActorRegistry<StorageActor>.Unregister(this);
    }

    /// <summary>
    /// Check if storage accepts items with given tag.
    /// </summary>
    public bool AcceptsTag(string tag) {
      return acceptedTags.Contains(tag);
    }

    /// <summary>
    /// Check if storage accepts items with all given tags.
    /// </summary>
    public bool AcceptsAllTags(string[] tags) {
      return tags.All(AcceptsTag);
    }

    /// <summary>
    /// Check if storage accepts items with any of given tags.
    /// </summary>
    public bool AcceptsAnyTag(string[] tags) {
      return tags.Any(AcceptsTag);
    }

    /// <summary>
    /// Check if storage has space for item with given tag.
    /// </summary>
    public bool HasSpaceFor(string tag) {
      return AcceptsTag(tag) && freeSlots > 0;
    }

    /// <summary>
    /// Check if storage has space for item with given tags.
    /// </summary>
    public bool HasSpaceFor(string[] tags) {
      return AcceptsAnyTag(tags) && freeSlots > 0;
    }

    /// <summary>
    /// Try to deposit item into storage.
    /// </summary>
    public bool TryDeposit(ActorDescription item) {
      return _inventory.TryPutItemInInventory(item);
    }

    /// <summary>
    /// Get count of items with specific tag in storage.
    /// </summary>
    public int GetCountWithTag(string tag) {
      return _inventory.GetTotalCountWithTags(new[] { tag });
    }

    private void OnValidate() {
      if (_inventory == null) _inventory = GetComponent<FilteredActorInventory>();
      if (_priority == null) _priority = GetComponent<ActorPriority>();
      if (_description == null) _description = GetComponent<ActorDescription>();
    }
  }
}
