using System;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
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
    public bool isOccupied => item != null || count > 0;
    public bool hasSpaceInStack => isStackable && stackData.current < stackData.max;
    public int spaceInStack => isStackable ? stackData.max - stackData.current : 0;

    public ActorInventory inventory => _inventory;

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
      
      // if (other.GetComponentInParent<ActorInventory>() == null) {
      //   Debug.LogError($"going to destroy fod item after stacking {other.name} ",
      //   other.transform.parent);
      //   DecayableActor.RemoveFrom(other.gameObject);
      //   UnityEngine.Object.Destroy(other.gameObject);
      // }

      _inventory.NotifyItemAdded(this);
      return true;
    }

    public bool Put(ActorDescription actorDescription) {
      if (isOccupied) return false;

      DecayableActor.RemoveFrom(actorDescription.gameObject);

      actorDescription.transform.SetParent(_root, false);
      actorDescription.gameObject.SetActive(false);
      item = actorDescription;
      stackData = item.GetStackData() ?? new StackData { max = 1, current = 1 };

      if (stackData.current < 1) stackData.current = 1;

      _inventory.NotifyItemAdded(this);
      return true;
    }

    public bool Release(out ActorDescription actorDescription) {
      return Release(out actorDescription, null);
    }

    public bool Release(out ActorDescription actorDescription, Vector3? dropPosition) {
      actorDescription = null;
      if (!isOccupied) return false;

      actorDescription = item;
      item.transform.SetParent(null, true);
      item.gameObject.SetActive(true);

      if (dropPosition.HasValue) {
        item.transform.position = dropPosition.Value;
      }

      DecayableActor.AttachTo(actorDescription.gameObject);

      item = null;
      stackData = null;

      _inventory.NotifyItemRemoved(this);
      return true;
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
        else {
          _inventory.NotifyItemRemoved(this);
        }
      }
      else {
        ClearSlot();
      }
    }

    /// <summary>
    /// Split count from stack, spawning a new item.
    /// </summary>
    /// <param name="count">How many to split off</param>
    /// <param name="spawner">Required to create new item instance</param>
    /// <param name="splitItem">The newly created item with specified count</param>
    /// <returns>True if split successful</returns>
    public bool TrySplit(int count, ActorCreationModule spawner, out ActorDescription splitItem) {
      splitItem = null;
      if (!isOccupied || count <= 0 || spawner == null) return false;
      if (stackData == null || stackData.current <= count) return false;

      if (!spawner.TrySpawnActorOnGround(item.actorKey, _root.position, out splitItem)) {
        return false;
      }

      var newStackData = splitItem.GetStackData();
      if (newStackData != null) newStackData.current = count;

      stackData.current -= count;
      _inventory.NotifyItemRemoved(this);
      return true;
    }

    internal void ClearSlot() {
      var hadItem = item != null;
      if (item != null) {
        Debug.LogError($"clearing slot and destroying item, {item.name}", item);
        DecayableActor.RemoveFrom(item.gameObject);
        UnityEngine.Object.Destroy(item.gameObject);
      }

      item = null;
      stackData = null;

      if (hadItem) _inventory.NotifyItemRemoved(this);
    }

    public void SetReferences(ActorInventory actorInventory, Transform slot) {
      _inventory = actorInventory;
      _root = slot;
    }
  }
}