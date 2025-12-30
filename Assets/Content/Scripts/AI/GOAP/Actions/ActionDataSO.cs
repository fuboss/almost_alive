using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;
using Sirenix.Serialization;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Actions {
  [CreateAssetMenu(fileName = "ActionData", menuName = "GOAP/ActionData", order = 0)]
  [HideReferenceObjectPicker]
  public class ActionDataSO : SerializedScriptableObject {
    public AgentActionData data;
    
    public AgentAction GetAction(IGoapAgent agent) {
      var builder = new AgentAction.Builder(data.name)
        .WithCost(data.cost)
        .WithStrategy(data.strategy.Create(agent))
        .AddPreconditions(data.preconditions.ConvertAll(agent.GetBelief))
        .AddEffects(data.effects.ConvertAll(agent.GetBelief));
      
      return builder.Build();
    }
  }
}