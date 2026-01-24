using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Game {
  [RequireComponent(typeof(ActorDescription))]
  public abstract class TagDefinition : SerializedMonoBehaviour {
    public abstract string Tag { get; }
  }
}