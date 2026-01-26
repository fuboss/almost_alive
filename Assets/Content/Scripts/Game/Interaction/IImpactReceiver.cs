using UnityEngine;

namespace Content.Scripts.Game.Interaction {
  public interface IImpactReceiver {
    bool canReceiveImpact { get; }
    void ReceiveImpact(float damage, Vector3 impactPoint, Vector3 impactDirection);
  }
}
