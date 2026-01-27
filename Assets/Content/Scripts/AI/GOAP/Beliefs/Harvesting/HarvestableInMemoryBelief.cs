using System;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game;
using Content.Scripts.Game.Harvesting;
using Sirenix.OdinInspector;

namespace Content.Scripts.AI.GOAP.Beliefs.Harvesting {
  /// <summary>
  /// True when agent remembers harvestable actors with yield available.
  /// Optionally checks distance.
  /// </summary>
  [Serializable, TypeInfoBox("True when agent remembers harvestable with yield in memory (or inverse: none available).")]
  public class HarvestableInMemoryBelief : AgentBelief {
    public bool checkDistance;
    [EnableIf("checkDistance")] public float maxDistance = 30f;
    public bool inverse;

    protected override Func<bool> GetCondition(IGoapAgentCore agent) {
      var sqrMaxDistance = maxDistance * maxDistance;
      
      return () => {
        var memory = agent.memory;
        var harvestables = memory.GetWithAllTags(new[] { Tag.HARVESTABLE });
        
        if (harvestables.Length == 0) return inverse;

        var agentPos = agent.position;
        
        foreach (var snapshot in harvestables) {
          if (snapshot.target == null) continue;
          
          // Check if has yield
          if (!HarvestModule.HasYield(snapshot.target)) continue;
          
          // Check distance if required
          if (checkDistance) {
            var sqrDist = (agentPos - snapshot.location).sqrMagnitude;
            if (sqrDist > sqrMaxDistance) continue;
          }
          
          // Found valid harvestable
          return !inverse;
        }
        
        return inverse;
      };
    }

    public override AgentBelief Copy() {
      return new HarvestableInMemoryBelief {
        checkDistance = checkDistance,
        maxDistance = maxDistance,
        inverse = inverse,
        name = name
      };
    }

    public override string GetPresenterString() {
      var distStr = checkDistance ? $"<{maxDistance}m" : "";
      return inverse ? $"!{name}{distStr}" : $"{name}{distStr}";
    }
  }
}
