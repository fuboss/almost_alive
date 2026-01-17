using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Agent.Memory.Descriptors;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Game {
  public interface IActorDescription {
    DescriptionData descriptionData { get; }
    bool collectable { get; }
    StackData GetStackData();
    bool HasAllTags(string[] tags);
    GameObject gameObject { get; }
    Transform transform { get; }
    
    T GetDefinition<T>() where T : TagDefinition;
    TagDefinition GetDefinition(string tag);
  }


}