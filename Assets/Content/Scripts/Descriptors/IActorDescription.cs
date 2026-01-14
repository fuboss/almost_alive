using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Agent.Memory.Descriptors;
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
    
    TagDefinition GetDefinition<T>() where T : TagDefinition;
    TagDefinition GeDefinition(string tag);
  }

  public abstract class TagDefinition : SerializedMonoBehaviour {
    public abstract string Tag { get; }
  }
}