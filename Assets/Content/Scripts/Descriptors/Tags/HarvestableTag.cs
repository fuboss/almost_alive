using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Game {
  public class HarvestableTag : TagDefinition {
    public override string Tag => AI.Tag.HARVESTABLE;

    [Required]
    public string harvestableActorKey;

    [MinValue(1)]
    public int maxHarvest = 10;

    [Tooltip("Time in seconds for full regrowth (0 to maxHarvest)")]
    [MinValue(1)]
    public float respawnTime = 300f;

    [Tooltip("Work required to harvest one unit")]
    [MinValue(0.1f)]
    public float workPerUnit = 2f;

    [Tooltip("Growth curve: X = time progress (0-1), Y = yield ratio (0-1)")]
    public AnimationCurve respawnCurve = AnimationCurve.Linear(0, 0, 1, 1);

    private void OnEnable() {
      ActorRegistry<HarvestableTag>.Register(this);
    }

    private void OnDisable() {
      ActorRegistry<HarvestableTag>.Unregister(this);
    }
  }
}
