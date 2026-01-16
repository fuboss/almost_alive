using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.Game;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Agent {
  public class ActorInventory : SerializedMonoBehaviour {
    [SerializeField] private List<InventorySlot> _slots = new();
    public int slotCount => _slots.Count;

    public IEnumerable<InventorySlot> occupiedSlots => _slots.Where(slot => slot.isOccupied);
    public IEnumerable<InventorySlot> freeSlots => _slots.Where(slot => !slot.isOccupied);
    public bool isFull => !freeSlots.Any();
    public bool isEmpty => !occupiedSlots.Any();
    public int freeSlotCount => freeSlots.Count();

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
    public virtual bool TryPutItemInInventory(ActorDescription target) {
      // First try to stack with existing item of same type
      var stackSlot = FindStackableSlot(target);
      if (stackSlot != null && stackSlot.TryStack(target)) {
        return true;
      }

      // No stackable slot found, try empty slot
      var freeSlot = FirstFreeSlot();
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