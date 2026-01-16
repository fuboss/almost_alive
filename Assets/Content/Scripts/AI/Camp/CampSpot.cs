using System.Collections.Generic;
using System.Linq;
using Content.Scripts.Game;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.Camp {
  public class CampSpot : MonoBehaviour {
    [ValueDropdown("Tags")] public string[] preferredTags;
    
    [ShowInInspector, ReadOnly] private ActorDescription _builtActor;

    public ActorDescription builtActor => _builtActor;
    public bool isEmpty => _builtActor == null;
    public Vector3 position => transform.position;
    public Quaternion rotation => transform.rotation;

    /// <summary>Check if this spot accepts any of the given tags.</summary>
    public bool AcceptsAny(params string[] tags) {
      if (preferredTags == null || preferredTags.Length == 0) return true; // accepts anything
      return tags.Any(t => preferredTags.Contains(t));
    }

    /// <summary>Assigns built actor to this spot.</summary>
    public void SetBuiltActor(ActorDescription actor) {
      if (_builtActor != null) {
        Debug.LogWarning($"[CampSpot] Actor already built at {name}", this);
        return;
      }
      _builtActor = actor;
      actor.transform.SetParent(transform);
      actor.transform.localPosition = Vector3.zero;
      actor.transform.localRotation = Quaternion.identity;
    }

    public void ClearActor() {
      if (_builtActor != null) {
        Destroy(_builtActor.gameObject);
        _builtActor = null;
      }
    }

    private IEnumerable<string> Tags() => Tag.ALL_TAGS;
  }
}
