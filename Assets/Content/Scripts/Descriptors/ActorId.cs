using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Game {
  public class ActorId : MonoBehaviour {
    [ShowInInspector, ReadOnly] private int _id = -1;

    public int id {
      get => _id;
      private set => _id = value;
    }

    private void Awake() {
      if (id == -1) {
        id = Registry<ActorId>.Register(this);
      }
    }

    private void OnDestroy() {
      Registry<ActorId>.Unregister(this);
    }

    public override string ToString() {
      return $"ActorId({id})";
    }
  }
}