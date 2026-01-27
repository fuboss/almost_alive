using UnityEngine;

namespace Content.Scripts.Game {
  public class HarvestableTag : TagDefinition {
    public override string Tag => AI.Tag.HARVESTABLE;

    public string harvestableActorKey;
    public int maxHarvest = 10;
    public float respawnTime = 300f; // in seconds
    public AnimationCurve respawnCurve = AnimationCurve.Linear(0, 0, 1, 1);
  }
}
