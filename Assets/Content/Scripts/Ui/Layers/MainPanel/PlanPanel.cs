using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Stats;
using TMPro;
using UnityEngine;
using UnityUtils;

namespace Content.Scripts.Ui.Layers.MainPanel {
  /// <summary>
  /// Displays current Goal, Action Plan, related stats and beliefs.
  /// Primary tab for AI debugging.
  /// </summary>
  public class PlanPanel : BaseInfoPanel {
    [Header("Goal")]
    [SerializeField] private TMP_Text _goalNameText;
    [SerializeField] private TMP_Text _goalPriorityText;
    [SerializeField] private GameObject _noGoalPlaceholder;
    [SerializeField] private GameObject _goalContainer;

    [Header("Actions")]
    [SerializeField] private Transform _actionsContainer;
    [SerializeField] private PlanActionItem _actionItemPrefab;

    [Header("Related Stats")]
    [SerializeField] private Transform _statsContainer;
    [SerializeField] private StatPresenter _statPrefab;

    [Header("Related Beliefs")]
    [SerializeField] private Transform _beliefsContainer;
    [SerializeField] private BeliefPresenter _beliefPrefab;

    public override void Repaint() {
      base.Repaint();
      if (agent?.agentBrain == null) {
        ShowNoAgent();
        return;
      }

      var brain = agent.agentBrain;
      RepaintGoal(brain);
      RepaintActions(brain);
      RepaintRelatedStats();
      RepaintRelatedBeliefs(brain);
    }

    private void ShowNoAgent() {
      _goalContainer.SetActive(false);
      _noGoalPlaceholder.SetActive(true);
      _actionsContainer.DestroyChildren();
      _statsContainer.DestroyChildren();
      _beliefsContainer.DestroyChildren();
    }

    private void RepaintGoal(IAgentBrain brain) {
      var goal = brain.currentGoal;
      var hasGoal = goal != null;

      _goalContainer.SetActive(hasGoal);
      _noGoalPlaceholder.SetActive(!hasGoal);

      if (hasGoal) {
        _goalNameText.text = goal.Name;
        _goalPriorityText.text = $"Priority: {goal.Priority:F2}";
      }
    }

    private void RepaintActions(IAgentBrain brain) {
      _actionsContainer.DestroyChildren();

      var plan = brain.actionPlan;
      if (plan?.actions == null || plan.actions.Count == 0) return;

      var actions = plan.actions.ToArray();
      for (int i = actions.Length - 1; i >= 0; i--) {
        var action = actions[i];
        var item = Instantiate(_actionItemPrefab, _actionsContainer);
        var displayIndex = actions.Length - i;
        var isActive = i == actions.Length - 1;
        item.Setup(displayIndex, action.name, action.cost, isActive);
      }
    }

    private void RepaintRelatedStats() {
      _statsContainer.DestroyChildren();
      if (agent?.body == null) return;

      var stats = agent.body.GetStatsInfo();
      foreach (var stat in stats) {
        if (stat is not FloatAgentStat floatStat) continue;
        if (!IsRelevantStat(floatStat.type)) continue;

        var item = Instantiate(_statPrefab, _statsContainer);
        item.Setup(stat);
      }
    }

    private bool IsRelevantStat(StatType type) {
      return type is StatType.FATIGUE or StatType.HUNGER or StatType.HEALTH or StatType.SLEEP;
    }

    private void RepaintRelatedBeliefs(IAgentBrain brain) {
      _beliefsContainer.DestroyChildren();

      var beliefs = brain.beliefs;
      if (beliefs == null) return;

      int count = 0;
      foreach (var kvp in beliefs) {
        if (count >= 6) break;
        var item = Instantiate(_beliefPrefab, _beliefsContainer);
        item.Setup(kvp.Value);
        count++;
      }
    }
  }
}
