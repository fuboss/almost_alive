using Content.Scripts.Building.Runtime;
using Content.Scripts.Game;
using UnityEngine;

namespace Content.Scripts.DebugPanel {
  public class DebugActionContext {
    public Vector3 worldPosition;
    public ActorDescription targetActor;
    public object customData;
    public GameObject genericTarget;
    public Structure targetStructure;
  }
}

