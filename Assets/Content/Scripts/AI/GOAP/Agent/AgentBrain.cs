using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Goals;
using Content.Scripts.AI.GOAP.Interruption;
using Content.Scripts.AI.GOAP.Planning;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Agent {
  /// <summary>
  /// Brain for human colonist agents.
  /// Adds interruption system and plan stack on top of base brain.
  /// </summary>
  public class AgentBrain : AgentBrainBase {
    [Header("Interruption")]
    [SerializeField] private InterruptionManager _interruptionManager = new();
    [SerializeField] private int _planStackMaxDepth = 3;

    [FoldoutGroup("Debug")] [ReadOnly] [ShowInInspector]
    private int _planStackCount => _planStack?.count ?? 0;

    public InterruptionManager interruptionManager => _interruptionManager;

    private PlanStack _planStack;
    private ITransientTargetAgent _transientAgent;
    private AgentGoal _forcedGoal;

    public override void Initialize(IGoapAgentCore agent) {
      base.Initialize(agent);
      _planStack = new PlanStack(_planStackMaxDepth);
      _transientAgent = agent as ITransientTargetAgent;
    }

    public override void Tick(float deltaTime) {
      if (!_initialized) return;
      _currentDeltaTime = deltaTime;

      ExecuteStuckDetection(deltaTime);
      ExecuteInterruptionCheck();
      ExecutePlanning();
      ExecuteMemory(deltaTime);
      ExecuteSensors(deltaTime);
    }

    // ═══════════════════════════════════════════════════════════════
    // INTERRUPTION
    // ═══════════════════════════════════════════════════════════════

    private void ExecuteInterruptionCheck() {
      if (_currentGoal == null || _actionPlan == null) return;

      // InterruptionManager expects IGoapAgent for full access
      if (_agent is not IGoapAgent fullAgent) return;

      if (_interruptionManager.CheckForInterruption(
            fullAgent, _currentGoal,
            out var interruptGoal,
            out var shouldSave)) {
        InterruptForGoal(interruptGoal, shouldSave);
      }
    }

    private void InterruptForGoal(AgentGoal newGoal, bool saveCurrent) {
      Debug.Log($"[Brain] Interrupting '{_currentGoal?.Name}' for '{newGoal?.Name}' (save: {saveCurrent})", this);

      if (saveCurrent && _planStack.canPush && _currentGoal != null && _actionPlan != null) {
        _planStack.TryPush(_currentGoal, _actionPlan);
        Debug.Log($"[Brain] Saved plan to stack. Depth: {_planStack.count}", this);
      }

      _currentAction?.OnStop();
      _forcedGoal = newGoal;

      _currentAction = null;
      _currentGoal = null;
      _actionPlan = null;
      
      if (_transientAgent != null) {
        _transientAgent.transientTarget = null;
      }
      _agent.navMeshAgent.ResetPath();
    }

    // ═══════════════════════════════════════════════════════════════
    // PLAN STACK
    // ═══════════════════════════════════════════════════════════════

    protected override bool TryResumePlan() {
      if (!_planStack.TryPop(out var saved)) return false;

      if (!ValidateSavedPlan(saved)) {
        Debug.Log($"[Brain] Saved plan '{saved.goal?.Name}' no longer valid, discarding", this);
        return false;
      }

      Debug.Log($"[Brain] Resuming saved plan '{saved.goal?.Name}'. Depth: {_planStack.count}", this);

      _currentGoal = saved.goal;
      _actionPlan = saved.plan;

      return true;
    }

    private bool ValidateSavedPlan(PlanStack.SavedPlan saved) {
      if (!saved.isValid) return false;
      if (saved.goal == null) return false;
      if (saved.plan.actions.Count == 0) return false;
      return true;
    }

    // ═══════════════════════════════════════════════════════════════
    // PLANNING OVERRIDE
    // ═══════════════════════════════════════════════════════════════

    protected override ActionPlan CalculatePlan() {
      if (_gPlanner == null) {
        Debug.LogError("[Brain] Planner is null", this);
        return null;
      }

      // Forced goal from interruption
      if (_forcedGoal != null) {
        var forcedPlan = _gPlanner.Plan(_agent, new HashSet<AgentGoal> { _forcedGoal }, _lastGoal);
        _forcedGoal = null;
        return forcedPlan;
      }

      return base.CalculatePlan();
    }

    protected override void OnPlanCleared() {
      if (_transientAgent != null) {
        _transientAgent.transientTarget = null;
      }
    }
  }
}
