using UnityEngine;

namespace Content.Scripts.Core {
  public interface IPrefabFactory<T> where T : MonoBehaviour {
    T Spawn(Vector3 position);
  }
}