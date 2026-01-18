using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Goals;
using UnityEngine;
using UnityEngine.UIElements;

namespace Content.Scripts.Editor.GOAPPlayground.Nodes {
  public class GoalNodeView : BaseGoapNode {
    public GoalSO goalData { get; }

    public GoalNodeView(GoalSO goal) : base(goal.name) {
      goalData = goal;
      AddToClassList("goal-node");
      
      // Header - green for goals
      titleContainer.style.backgroundColor = new Color(0.2f, 0.5f, 0.3f);
      titleContainer.style.height = 22;
      
      // Compact title
      var titleLabel = titleContainer.Q<Label>("title-label");
      if (titleLabel != null) {
        titleLabel.style.fontSize = 11;
        titleLabel.text = goal.name.Replace("goal_", "");
        titleLabel.tooltip = goal.name;
      }
      
      // Bias badge
      var biasBadge = new Label($"P:{goal.template.utilityBias:F1}") {
        style = {
          fontSize = 9,
          color = new Color(0.8f, 0.9f, 0.8f),
          marginLeft = 5,
          unityFontStyleAndWeight = FontStyle.Bold
        }
      };
      titleContainer.Add(biasBadge);

      CreatePorts();
      
      // Make node compact
      style.minWidth = 120;
      style.maxWidth = 180;
      
      RefreshExpandedState();
      RefreshPorts();
    }

    private void CreatePorts() {
      // Execution in only
      CreateExecInPort();
      
      // Desired effects as inputs
      if (goalData.template.desiredEffects != null) {
        foreach (var effect in goalData.template.desiredEffects) {
          CreateBeliefInput(effect);
        }
      }
    }

    public IEnumerable<string> GetDesiredEffects() => goalData.template.desiredEffects ?? new List<string>();
  }
}
