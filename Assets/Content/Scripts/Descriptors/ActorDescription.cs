using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Agent.Memory.Descriptors;
using Content.Scripts.Game.Interaction;
using Content.Scripts.World.Grid;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Game {
  [RequireComponent(typeof(ActorId))]
  public class ActorDescription : SerializedMonoBehaviour, IActorDescription, ISelectableActor {
    public string actorKey;

    [SerializeField] private DescriptionData _descriptionData;
    public bool isSelectable;
    [ShowInInspector, ReadOnly] private bool _isSelected;
    [SerializeField, HideInInspector] private TagDefinition[] _tagDefinitions;

    public DescriptionData descriptionData => _descriptionData;
    public bool collectable => GetDefinition<ItemTag>() != null;
    public int actorId => GetComponent<ActorId>().id;
    public bool canSelect => isSelectable;

    public bool isSelected {
      get => _isSelected;
      set => _isSelected = value;
    }

    public StackData GetStackData() {
      return GetDefinition<ItemTag>()?.stackData;
    }

    public bool HasAllTags(string[] tags) {
      if (_descriptionData?.tags == null) {
        Debug.LogError("DescriptionData is null", this);
        return false;
      }
      return _descriptionData.tags.Length == 0 || tags.All(t => _descriptionData.tags.Contains(t));
    }
    public bool HasAnyTags(string[] resourcesTags) {
      return resourcesTags.Any(t => _descriptionData.tags.Contains(t));
    }

    public T GetDefinition<T>() where T : TagDefinition {
      return GetComponent<T>();
    }

    public TagDefinition GetDefinition(string tag) {
      return _tagDefinitions.FirstOrDefault(td => td.Tag == tag);
    }

    private void OnValidate() {
      _tagDefinitions = GetComponents<TagDefinition>();
      if (_descriptionData != null) {
        _descriptionData.tags = _tagDefinitions.Select(td => td.Tag).ToArray();
      }
    }

    private void OnEnable() {
      WorldGrid.Register(this);
    }

    private void OnDisable() {
      WorldGrid.Unregister(this);
    }

    public bool HasTag(string tag) {
      return _descriptionData?.tags != null && _descriptionData.tags.Contains(tag);
    }
  }
}