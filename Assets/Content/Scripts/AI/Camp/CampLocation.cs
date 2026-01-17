using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.Camp {
  public class CampLocation : MonoBehaviour {
    [ShowInInspector, ReadOnly] private GOAPAgent _owner;
    [ShowInInspector, ReadOnly] private CampSetup _setup;

    public GOAPAgent owner => _owner;
    public CampSetup setup => _setup;
    public bool isClaimed => _owner != null;
    public bool hasSetup => _setup != null;

    private void OnEnable() => Registry<CampLocation>.Register(this);
    private void OnDisable() => Registry<CampLocation>.Unregister(this);

    public bool TryClaim(GOAPAgent agent) {
      if (isClaimed) return false;
      _owner = agent;
      Debug.LogError($"[CampLocation] Claim {name}", this);
      return true;
    }

    public void AssignSetup(CampSetup setup) {
      if (_setup != null) {
        Debug.LogWarning($"[CampLocation] Setup already assigned to {name}", this);
        return;
      }

      _setup = setup;
      _setup.transform.SetParent(transform);
      _setup.transform.localPosition = Vector3.zero;
      _setup.transform.localRotation = Quaternion.identity;
      Debug.LogError($"[CampLocation] Setup {setup.name}assigned to {name}", this);
    }

    public void Release() {
      Debug.LogError($"[CampLocation] Release setup {name}", this);
      if (_setup != null) {
        Destroy(_setup.gameObject);
        _setup = null;
      }

      _owner = null;
    }
  }
}