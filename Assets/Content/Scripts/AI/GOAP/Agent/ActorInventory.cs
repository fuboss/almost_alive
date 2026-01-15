using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.Game;
using Content.Scripts.Game.Decay;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Agent {
  public class ActorInventory : SerializedMonoBehaviour {
    [SerializeField] private List<InventorySlot> _slots = new();
    public int slotCount => _slots.Count;

    public IEnumerable<InventorySlot> occupiedSlots {
      get { return _slots.Where(slot => slot.isOccupied); }
    }

    public IEnumerable<InventorySlot> freeSlots {
      get { return _slots.Where(slot => !slot.isOccupied); }
    }

    public InventorySlot FirstFreeSlot() {
      return _slots.FirstOrDefault(slot => !slot.isOccupied);
    }

    public InventorySlot GetSlot(int index) {
      return _slots.FirstOrDefault(slot => slot.index == index);
    }

    public virtual bool TryPutItemInInventory(ActorDescription target) {
      var slot = FirstFreeSlot();
      if (slot != null) {
        slot.Put(target);
        return true;
      }

      return AddItemToSlot(target, 0);
    }

    public bool AddItemToSlot(ActorDescription item, int slotIndex, int count = 1) {
      var slot = GetSlot(slotIndex);
      if (slot == null) return false;
      if (slot.isOccupied) {
        if (slot.item != item || !slot.isStackable) return AddItemToSlot(item, slotIndex + 1, count);
        slot.stackData.current += count;
        if (slot.stackData.current <= slot.stackData.max) return true;

        var delta = slot.stackData.current - slot.stackData.max;
        slot.stackData.current = slot.stackData.max;

        while (delta > 0) {
          if (AddItemToSlot(item, slotIndex + 1, count)) {
            delta = 0;
          }
        }

        return true;
      }

      slot.Put(item);
      return true;
    }


    //todo: create indexer
    public bool TryGetSlotWithItemTags(string[] tags, out InventorySlot foundSlot) {
      foundSlot = null;
      if (tags == null || tags.Length == 0) {
        return false;
      }

      foreach (var slot in occupiedSlots) {
        if (!slot.item.HasAllTags(tags)) continue;
        foundSlot = slot;
        return true;
      }

      foundSlot = null;
      return false;
    }

    private void Awake() {
      InitInventorySlots();
    }

    private void InitInventorySlots() {
      foreach (var inventorySlot in _slots) {
        inventorySlot.index = _slots.IndexOf(inventorySlot);
        var slot = new GameObject($"slot_{inventorySlot.index}").transform;
        slot.SetParent(transform, false);
        inventorySlot.SetReferences(this, slot);
      }
    }

    public int GetTotalCountWithTags(string[] tags) {
      int count = 0;

      foreach (var slot in occupiedSlots) {
        if (!slot.item.HasAllTags(tags)) continue;
        count += slot.count;
      }

      return count;
    }
  }

  [Serializable]
  public class InventorySlot {
    public int index;
    public ActorDescription item;
    public bool isStackable => stackData != null;
    public StackData stackData;
    private ActorInventory _inventory;
    private Transform _root;
    public int count => stackData?.current ?? (item != null ? 1 : 0);
    public bool isOccupied => item != null;

    public bool Put(ActorDescription actorDescription) {
      if (isOccupied) return false;

      // Remove decay component when picking up
      DecayableActor.RemoveFrom(actorDescription.gameObject);

      actorDescription.transform.SetParent(_root, false);
      actorDescription.gameObject.SetActive(false);
      item = actorDescription;
      stackData = item.GetStackData();

      Debug.Log($"[Inventory]{actorDescription.name} put in slot {index}", _root);
      return true;
    }

    public bool Release(out ActorDescription actorDescription) {
      return Release(out actorDescription, null);
    }

    public bool Release(out ActorDescription actorDescription, Vector3? dropPosition) {
      actorDescription = item;
      if (!isOccupied) return false;

      if (item != null) {
        item.transform.SetParent(null, true);
        item.gameObject.SetActive(true);

        if (dropPosition.HasValue) {
          item.transform.position = dropPosition.Value;
        }

        // Add decay component when dropping
        DecayableActor.AttachTo(item.gameObject);
      }

      var releasedName = item?.name ?? "null";
      item = null;
      stackData = null;

      Debug.Log($"[Inventory]slot {index} released: {releasedName}", _root);
      return true;
    }

    public void SetReferences(ActorInventory actorInventory, Transform slot) {
      _inventory = actorInventory;
      _root = slot;
    }
  }

  [Serializable]
  public class StackData {
    public int min = 0;
    public int max = 10;
    public int current = 0;
  }
}