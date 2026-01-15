using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Agent.Memory.Descriptors;
using Content.Scripts.Game.Interaction;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Game {
  [RequireComponent(typeof(ActorId))]
  public class ActorDescription : SerializedMonoBehaviour, IActorDescription, ISelectableActor {
    [SerializeField] private DescriptionData _descriptionData;
    public bool isSelectable;
    [ShowInInspector, ReadOnly] private bool _isSelected;
    [SerializeField, HideInInspector] private TagDefinition[] _tagDefinitions;

    public DescriptionData descriptionData => _descriptionData;
    public bool collectable => GetDefinition<ItemTag>() != null;

    public StackData GetStackData() {
      return GetDefinition<ItemTag>()?.stackData;
    }

    public bool HasAllTags(string[] tags) {
      return tags.All(t => _descriptionData.tags.Contains(t));
    }

    public T GetDefinition<T>() where T : TagDefinition {
      return GetComponent<T>();
    }
    
    public TagDefinition GetDefinition(string tag) {
      return _tagDefinitions.FirstOrDefault(td => td.Tag == tag);
    }

    public bool canSelect => isSelectable;

    public bool isSelected {
      get => _isSelected;
      set => _isSelected = value;
    }

    private void OnValidate() {
      _tagDefinitions = GetComponents<TagDefinition>();
      _descriptionData.tags = _tagDefinitions.Select(td => td.Tag).ToArray();
    }
    
    
  }
}