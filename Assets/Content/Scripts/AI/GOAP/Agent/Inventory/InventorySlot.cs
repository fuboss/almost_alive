using System;
using System.Linq;
using Content.Scripts.Game;
using Content.Scripts.Game.Decay;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Agent {
  [Serializable]
  public class InventorySlot {
    public int index;
    public ActorDescription item;
    public StackData stackData;

    private ActorInventory _inventory;
    private Transform _root;

    public bool isStackable => stackData != null && stackData.max > 1;
    public int count => stackData?.current ?? (item != null ? 1 : 0);
    public bool isOccupied => item != null;
    public bool hasSpaceInStack => isStackable && stackData.current < stackData.max;
    public int spaceInStack => isStackable ? stackData.max - stackData.current : 0;

    /// <summary>
    /// Check if this slot can stack with given item.
    /// Items are stackable if they have same tags and slot has space.
    /// </summary>
    public bool CanStackWith(ActorDescription other) {
      if (!isOccupied || !isStackable || !hasSpaceInStack) return false;
      if (other == null) return false;

      // Check if other item is stackable
      var otherStack = other.GetStackData();
      if (otherStack == null) return false;

      // Compare tags to determine if same type
      return HaveSameTags(item, other);
    }

    private static bool HaveSameTags(ActorDescription a, ActorDescription b) {
      var tagsA = a.descriptionData.tags;
      var tagsB = b.descriptionData.tags;

      if (tagsA == null || tagsB == null) return false;
      if (tagsA.Length != tagsB.Length) return false;

      // Simple comparison - both must have exactly same tags
      return tagsA.All(t => tagsB.Contains(t));
    }

    /// <summary>
    /// Try to stack item. Destroys the item GameObject on success.
    /// </summary>
    public bool TryStack(ActorDescription other, int addCount = 1) {
      if (!CanStackWith(other)) return false;

      var toAdd = Mathf.Min(addCount, spaceInStack);
      stackData.current += toAdd;

      // Destroy the added item since we're stacking
      DecayableActor.RemoveFrom(other.gameObject);
      UnityEngine.Object.Destroy(other.gameObject);

      Debug.Log($"[Inventory] Stacked +{toAdd} to slot {index}, total: {stackData.current}", _root);
      return true;
    }

    public bool Put(ActorDescription actorDescription) {
      if (isOccupied) return false;

      DecayableActor.RemoveFrom(actorDescription.gameObject);

      actorDescription.transform.SetParent(_root, false);
      actorDescription.gameObject.SetActive(false);
      item = actorDescription;
      stackData = item.GetStackData() ?? new StackData { max = 1, current = 1 };

      // Ensure current is at least 1
      if (stackData.current < 1) stackData.current = 1;

      Debug.Log($"[Inventory] {actorDescription.name} put in slot {index}", _root);
      return true;
    }

    public bool Release(out ActorDescription actorDescription) {
      return Release(out actorDescription, null);
    }

    public bool Release(out ActorDescription actorDescription, Vector3? dropPosition) {
      actorDescription = null;
      if (!isOccupied) return false;

      // If stacked, decrease count
      if (isStackable && stackData.current > 1) {
        // TODO: Need prefab reference to spawn new instance
        // For now, release entire stack
        Debug.LogWarning($"[Inventory] Releasing entire stack of {stackData.current} items");
      }

      actorDescription = item;
      item.transform.SetParent(null, true);
      item.gameObject.SetActive(true);

      if (dropPosition.HasValue) {
        item.transform.position = dropPosition.Value;
      }

      DecayableActor.AttachTo(actorDescription.gameObject);

      var releasedName = item.name;
      item = null;
      stackData = null;

      Debug.Log($"[Inventory] Slot {index} released: {releasedName}", _root);
      return true;
    }

    /// <summary>
    /// Release single item from stack. Returns null if need to spawn copy.
    /// </summary>
    public bool ReleaseSingle(out ActorDescription actorDescription, Vector3? dropPosition = null) {
      actorDescription = null;
      if (!isOccupied) return false;

      if (!isStackable || stackData.current <= 1) {
        return Release(out actorDescription, dropPosition);
      }

      // Has multiple - decrease count but keep slot occupied
      stackData.current--;
      Debug.Log($"[Inventory] Released 1 from stack, remaining: {stackData.current}");

      // Cannot return actual item - would need to spawn copy
      // Return null to indicate caller needs to handle spawning
      return false;
    }

    /// <summary>
    /// Remove count from stack. Clears slot if count reaches zero.
    /// </summary>
    public void RemoveCount(int amount) {
      if (!isOccupied || amount <= 0) return;

      if (stackData != null) {
        stackData.current -= amount;
        if (stackData.current <= 0) {
          ClearSlot();
        }
      }
      else {
        ClearSlot();
      }
    }

    internal void ClearSlot() {
      if (item != null) {
        DecayableActor.RemoveFrom(item.gameObject);
        UnityEngine.Object.Destroy(item.gameObject);
      }
      item = null;
      stackData = null;
    }

    public void SetReferences(ActorInventory actorInventory, Transform slot) {
      _inventory = actorInventory;
      _root = slot;
    }
  }
}