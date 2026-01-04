using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Agent.Memory.Descriptors;
using Content.Scripts.Game.Interaction;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Game {
  public interface IActorDescription {
    DescriptionData descriptionData { get; }
    bool canPickup { get; }
    StackData GetStackData();
    bool HasAllTags(string[] tags);
    GameObject gameObject { get; }
    Transform transform { get; }
  }

  public class ActorDescription : SerializedMonoBehaviour, IActorDescription, ISelectableActor {
    [SerializeField] private DescriptionData _descriptionData;
    public bool isSelectable;
    [ShowInInspector, ReadOnly] private bool _isSelected;

    public DescriptionData descriptionData => _descriptionData;
    public bool canPickup => descriptionData.isInventoryItem;

    public StackData GetStackData() {
      descriptionData.stackData ??= new StackData { max = 1, current = 1 };
      return descriptionData.stackData;
    }

    public bool HasAllTags(string[] tags) {
      return tags.All(t => _descriptionData.tags.Contains(t));
    }

    public bool canSelect => isSelectable;

    public bool isSelected {
      get => _isSelected;
      set => _isSelected = value;
    }
  }
}