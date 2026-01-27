using UnityEngine;

namespace Content.Scripts.Game {
  public class TreeTag : TagDefinition {
    public string woodActorID = "wood_0";
    public string logActorKey;
    public int woodYield = 4;
    public float workRequired = 10f;
    
    [Header("Fall Physics")]
    public float mass = 50f;
    [Tooltip("Optional: position for leaf burst effect (top of tree)")]
    public Transform crownTransform;

    public override string Tag => AI.Tag.TREE;

    private void OnEnable() {
      ActorRegistry<TreeTag>.Register(this);
    }

    private void OnDisable() {
      ActorRegistry<TreeTag>.Unregister(this);
    }
  }
}