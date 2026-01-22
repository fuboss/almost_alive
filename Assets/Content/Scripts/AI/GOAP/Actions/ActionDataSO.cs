using System;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Content.Scripts.AI.GOAP.Actions {
  [CreateAssetMenu(fileName = "ActionData", menuName = "GOAP/ActionData", order = 0)]
  [HideReferenceObjectPicker]
  public class ActionDataSO : SerializedScriptableObject {
    [PropertyOrder(-1)]
    [Button("Validate", ButtonSizes.Small), GUIColor(0.4f, 0.8f, 0.4f)]
    private void Validate() {
#if UNITY_EDITOR
      var allRefs = data.preconditions.Concat(data.effects);
      var missing = GOAPEditorHelper.ValidateBeliefReferences(allRefs);

      if (missing.Count > 0) {
        Debug.LogError($"[{name}] Missing beliefs: {string.Join(", ", missing)}", this);
      }
      else {
        Debug.Log($"[{name}] All beliefs valid ✓", this);
      }
#endif
    }

    public AgentActionData data;

    public AgentAction GetAction(IGoapAgentCore agent, IObjectResolver objectResolver) {
      if (data == null) {
        Debug.LogError($"ERROR {name}", this);
        return null;
      }

      var builder = new AgentAction.Builder(data.name);
      try {
        IActionStrategy strategy = data.strategy.Create(agent);
        objectResolver.Inject(strategy);
        builder.WithCost(data.cost)
          .WithBenefit(data.benefit)
          .WithStrategy(strategy)
          .AddPreconditions(data.preconditions.ConvertAll(agent.GetBelief))
          .AddEffects(data.effects.ConvertAll(agent.GetBelief));
      }
      catch (Exception e) {
        Debug.LogException(e, this);
      }
      return builder.Build();
    }

    private void OnValidate() {
      if (data != null) {
        data.name = name;
      }
    }
  }
}
