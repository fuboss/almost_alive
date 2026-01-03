using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Agent.Descriptors {
  public class ActorDescription : SerializedMonoBehaviour {
    [SerializeField] private DescriptionData _descriptionData;

    public DescriptionData descriptionData => _descriptionData;
    public bool canPickup => descriptionData.isInventoryItem;

    public StackData GetStackData() {
      descriptionData.stackData ??= new StackData { max = 1, current = 1 };
      return descriptionData.stackData;
    }

    public bool HasAllTags(string[] tags) {
      return tags.All(t => _descriptionData.tags.Contains(t));
    }
  }
}