using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Agent.Memory;
using Content.Scripts.AI.GOAP.Agent.Memory.Query;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Content.Scripts.Ui.Layers.MainPanel {
  public class DebugPanel : BaseInfoPanel {
    [Header("Memory")]
    [SerializeField] private TMP_Text _memoryCountText;
    [SerializeField] private TMP_Text _consolidationStatsText;
    [SerializeField] private TMP_Text _sensorStatsText;
    
    [Header("Planning")]
    [SerializeField] private TMP_Text _plannerStatsText;
    [SerializeField] private TMP_Text _currentGoalText;
    [SerializeField] private TMP_Text _currentActionText;
    [SerializeField] private Transform _planActionsContainer;
    [SerializeField] private TMP_Text _actionItemPrefab;

    [Header("Memory List")]
    [SerializeField] private Transform _memoryListContainer;
    [SerializeField] private MemoryItemPresenter _memoryItemPrefab;
    [SerializeField] private ScrollRect _memoryScrollRect;

    
    private MemoryConsolidationModule _consolidation;
    // private IncrementalPlanner _planner;
    // private SensorProcessorModule _sensorProcessor;
    // public void InjectModules(SensorProcessorModule sensorProcessor, 
    //   MemoryConsolidationModule consolidation, IncrementalPlanner planner) {
    //   
    //   _sensorProcessor = sensorProcessor;
    //   _consolidation = consolidation;
    //   _planner = planner;
    // }
    
    public override void Repaint() {
      base.Repaint();
      
      if (agent == null) {
        ClearAll();
        return;
      }

      UpdateMemoryStats();
      UpdatePlanningStats();
      UpdateMemoryList();
    }

    private void UpdateMemoryStats() {
      var memory = agent.memory;
      var allMemories = memory.Query().Execute(memory);
      
      _memoryCountText.text = $"Total Memories: {allMemories.Length}";

      if (_consolidation != null) {
        var stats = _consolidation.GetStats();
        _consolidationStatsText.text = 
          $"Forgotten: {stats.ForgottenCount}\n" +
          $"Next Reinforcement: {stats.TimeSinceReinforcement:F1}s";
      }

      // if (_sensorProcessor != null) {
      //   var stats = _sensorProcessor.GetStats();
      //   _sensorStatsText.text = 
      //     $"Created: {stats.MemoriesCreated}\n" +
      //     $"Updated: {stats.MemoriesUpdated}";
      // }
    }

    private void UpdatePlanningStats() {
      var brain = agent.agentBrain;
      
      _currentGoalText.text = brain.actionPlan?.agentGoal?.Name ?? "None";
      _currentActionText.text = GetCurrentActionName(brain);

      // if (_planner != null) {
      //   var stats = _planner.GetStats();
      //   _plannerStatsText.text = 
      //     $"Cache: {stats.CacheSize}/{_planner.maxCacheSize}\n" +
      //     $"Hit Rate: {stats.HitRate:P0}\n" +
      //     $"Hits: {stats.CacheHits} | Misses: {stats.CacheMisses}";
      // }

      UpdatePlanActions(brain);
    }

    private string GetCurrentActionName(AgentBrain brain) {
      if (brain.actionPlan == null) return "None";
      
      var remaining = brain.actionPlan.actions.Count;
      if (remaining == 0) return "Completing...";

      var next = brain.actionPlan.actions.Peek();
      return $"{next.name} (+{remaining - 1} more)";
    }

    private void UpdatePlanActions(AgentBrain brain) {
      // Clear existing
      foreach (Transform child in _planActionsContainer) {
        Destroy(child.gameObject);
      }

      if (brain.actionPlan == null) return;

      var actions = brain.actionPlan.actions.ToArray();
      for (int i = actions.Length - 1; i >= 0; i--) {
        var action = actions[i];
        var item = Instantiate(_actionItemPrefab, _planActionsContainer);
        item.text = $"{actions.Length - i}. {action.name} (cost: {action.cost})";
      }
    }

    private void UpdateMemoryList() {
      // Clear existing
      foreach (Transform child in _memoryListContainer) {
        Destroy(child.gameObject);
      }

      var memories = agent.memory.Query()
        .OrderBy(s => -s.confidence)
        .Take(20)
        .Execute(agent.memory);

      foreach (var memory in memories) {
        var item = Instantiate(_memoryItemPrefab, _memoryListContainer);
        item.Setup(memory);
      }
    }

    private void ClearAll() {
      _memoryCountText.text = "No agent selected";
      _consolidationStatsText.text = "";
      _sensorStatsText.text = "";
      _plannerStatsText.text = "";
      _currentGoalText.text = "None";
      _currentActionText.text = "None";
    }
  }
}