using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Goals {
  [CreateAssetMenu(fileName = "Goal", menuName = "GOAP/Goal", order = 0)]
  public class GoalSO : SerializedScriptableObject {
    public int defaultPriority = 1;
    [ValueDropdown("GetEffectNames")] public List<string> desiredEffects = new();

    public AgentGoal Get(IGoapAgent agent) {
      var builder = new AgentGoal.Builder(name)
        .WithDesiredEffects(desiredEffects.Select(agent.GetBelief))
        .WithPriority(defaultPriority);

      return builder.Build();
    }
#if UNITY_EDITOR
    public List<string> GetEffectNames() {
      return GOAPEditorHelper.GetBeliefsNames();
    }
#endif
  }
}