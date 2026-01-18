using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Actions;
using UnityEngine;
using UnityEngine.UIElements;

namespace Content.Scripts.Editor.GOAPPlayground.Nodes {
  public class ActionNodeView : BaseGoapNode {
    public ActionDataSO actionData { get; }
    public float cost => actionData?.data?.cost ?? 1;
    public float benefit => actionData?.data?.benefit ?? 1;

    public ActionNodeView(ActionDataSO action) : base(action.name) {
      actionData = action;
      AddToClassList("action-node");
      
      // Header style
      titleContainer.style.backgroundColor = new Color(0.2f, 0.35f, 0.55f);
      titleContainer.style.height = 22;
      
      // Compact title
      var titleLabel = titleContainer.Q<Label>("title-label");
      if (titleLabel != null) {
        titleLabel.style.fontSize = 11;
        titleLabel.text = ShortenActionName(action.name);
        titleLabel.tooltip = action.name;
      }
      
      // Score badge
      var score = benefit / Mathf.Max(cost, 0.1f);
      var scoreBadge = new Label($"S:{score:F1}") {
        style = {
          fontSize = 9,
          color = new Color(0.8f, 0.8f, 0.5f),
          marginLeft = 5,
          unityFontStyleAndWeight = FontStyle.Bold
        }
      };
      titleContainer.Add(scoreBadge);

      CreatePorts();
      
      // Make node more compact
      style.minWidth = 140;
      style.maxWidth = 200;
      
      RefreshExpandedState();
      RefreshPorts();
    }

    private string ShortenActionName(string name) {
      return name.Replace("action_", "")
                 .Replace("_", " ");
    }

    private void CreatePorts() {
      // Execution flow ports (white)
      CreateExecInPort();
      CreateExecOutPort();

      // Preconditions (orange inputs)
      if (actionData.data.preconditions != null) {
        foreach (var pre in actionData.data.preconditions) {
          CreateBeliefInput(pre);
        }
      }

      // Effects (green outputs)
      if (actionData.data.effects != null) {
        foreach (var eff in actionData.data.effects) {
          CreateBeliefOutput(eff);
        }
      }
    }

    public IEnumerable<string> GetPreconditions() => actionData.data.preconditions ?? new List<string>();
    public IEnumerable<string> GetEffects() => actionData.data.effects ?? new List<string>();
  }
}
