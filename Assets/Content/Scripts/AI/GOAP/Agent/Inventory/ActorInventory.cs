using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.Game;
using Sirenix.OdinInspector;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Content.Scripts.AI.GOAP.Agent {
  public class ActorInventory : SerializedMonoBehaviour {
    [SerializeField] private List<InventorySlot> _slots = new();
    public int slotCount => _slots.Count;

    public event Action<InventorySlot> OnItemAdded;
    public event Action<InventorySlot> OnItemRemoved;
    public event Action OnInventoryChanged;

    public IEnumerable<InventorySlot> occupiedSlots {
      get {
        foreach (var slot in _slots) {
          if (slot.isOccupied) yield return slot;
        }
      }
    }

    public IEnumerable<InventorySlot> freeSlots {
      get {
        foreach (var slot in _slots) {
          if (!slot.isOccupied) yield return slot;
        }
      }
    }

    public bool isFull {
      get {
        foreach (var slot in _slots) {
          if (!slot.isOccupied) return false;
        }
        return true;
      }
    }

    public bool isEmpty {
      get {
        foreach (var slot in _slots) {
          if (slot.isOccupied) return false;
        }
        return true;
      }
    }

    public int freeSlotCount {
      get {
        var count = 0;
        foreach (var slot in _slots) {
          if (!slot.isOccupied) count++;
        }
        return count;
      }
    }

    public bool hasAnyFreeSlot {
      get {
        foreach (var slot in _slots) {
          if (!slot.isOccupied) return true;
        }
        return false;
      }
    }

    public InventorySlot FirstFreeSlot() {
      return _slots.FirstOrDefault(slot => !slot.isOccupied);
    }

    public InventorySlot GetSlot(int index) {
      return _slots.FirstOrDefault(slot => slot.index == index);
    }

    /// <summary>
    /// Try to put item in inventory. First tries to stack with existing items,
    /// then uses empty slot if stacking not possible.
    /// </summary>
    public virtual bool TryPutItemInInventory(ActorDescription target, int count = 1) {
      // First try to stack with existing item of same type
      var stackSlot = FindStackableSlot(target);
      if (stackSlot != null && stackSlot.TryStack(target, count)) {
        return true;
      }

      // No stackable slot found, try empty slot
      var freeSlot = FirstFreeSlot();
      Debug.Log($"[{GetType().Name}] Trying to put item '{target.actorKey}' in free slot {freeSlot?.index}", this);
      return freeSlot != null && freeSlot.Put(target);
    }

    /// <summary>
    /// Find slot that can stack with given item.
    /// </summary>
    private InventorySlot FindStackableSlot(ActorDescription item) {
      return occupiedSlots.FirstOrDefault(slot => slot.CanStackWith(item));
    }

    public bool TryGetSlotWithItemTags(string[] tags, out InventorySlot foundSlot) {
      foundSlot = null;
      if (tags == null || tags.Length == 0) return false;

      foreach (var slot in occupiedSlots) {
        if (!slot.item.HasAllTags(tags)) continue;
        foundSlot = slot;
        return true;
      }

      return false;
    }

    private void Awake() {
      InitInventorySlots();
    }

    private void InitInventorySlots() {
      for (var i = 0; i < _slots.Count; i++) {
        var slot = _slots[i];
        slot.index = i;
        var slotTransform = new GameObject($"slot_{i}").transform;
        slotTransform.SetParent(transform, false);
        slot.SetReferences(this, slotTransform);
      }
    }

    public int GetTotalCountWithTags(string[] tags) {
      return occupiedSlots
        .Where(slot => slot.item.HasAllTags(tags))
        .Sum(slot => slot.count);
    }

    /// <summary>Get total count of items with specific tag.</summary>
    public int GetItemCount(string tag) {
      return GetTotalCountWithTags(new[] { tag });
    }

    internal void NotifyItemAdded(InventorySlot slot) {
      OnItemAdded?.Invoke(slot);
      OnInventoryChanged?.Invoke();
    }

    internal void NotifyItemRemoved(InventorySlot slot) {
      OnItemRemoved?.Invoke(slot);
      OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// Transfer items with specified tag to another inventory.
    /// Handles full stack transfer and partial stack splitting.
    /// </summary>
    /// <param name="target">Target inventory to transfer to</param>
    /// <param name="tag">Tag to match items</param>
    /// <param name="maxCount">Maximum count to transfer</param>
    /// <param name="spawner">Required for partial stack splitting</param>
    /// <returns>Actual count transferred</returns>
    public int TransferTo(ActorInventory target, string tag, int maxCount, ActorCreationModule spawner = null) {
      if (target == null || maxCount <= 0) return 0;

      if (!TryGetSlotWithItemTags(new[] { tag }, out var slot)) return 0;

      var available = slot.count;
      var toTransfer = Mathf.Min(available, maxCount);

      // Full stack transfer
      if (toTransfer >= available) {
        if (!slot.Release(out var item)) return 0;
        
        if (target.TryPutItemInInventory(item)) {
          Debug.Log($"[Inventory] Transferred {toTransfer}x {tag}", this);
          return toTransfer;
        }
        
        // Failed - return item back
        slot.Put(item);
        return 0;
      }

      // Partial transfer - need to spawn new item
      if (spawner == null) {
        Debug.LogError("[Inventory] Spawner required for partial stack transfer");
        return 0;
      }

      if (!spawner.TrySpawnActorOnGround(slot.item.actorKey, target.transform.position, out var newItem)) {
        Debug.LogError($"[Inventory] Failed to spawn item for partial transfer");
        return 0;
      }

      var stackData = newItem.GetStackData();
      if (stackData != null) stackData.current = toTransfer;

      if (target.TryPutItemInInventory(newItem, toTransfer)) {
        slot.RemoveCount(toTransfer);
        Debug.Log($"[Inventory] Transferred {toTransfer}x {tag} (partial)", this);
        return toTransfer;
      }

      // Failed - destroy spawned item
      Debug.LogError($"clearing slot and destroying item, {newItem.name}", newItem);
      Destroy(newItem.gameObject);
      return 0;
    }
  }

  [Serializable]
  public class StackData {
    public int min = 0;
    public int max = 10;
    public int current = 1;

    public StackData Clone() {
      return new StackData { min = min, max = max, current = current };
    }
  }
}