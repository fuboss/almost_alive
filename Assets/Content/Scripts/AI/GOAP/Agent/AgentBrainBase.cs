using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent.Memory;
using Content.Scripts.AI.GOAP.Agent.Sensors;
using Content.Scripts.AI.GOAP.Beliefs;
using Content.Scripts.AI.GOAP.Goals;
using Content.Scripts.AI.GOAP.Planning;
using Content.Scripts.AI.Navigation;
using Content.Scripts.Game;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;
using Random = UnityEngine.Random;

namespace Content.Scripts.AI.GOAP.Agent {
  /// <summary>
  /// Base class for agent brains. Handles core GOAP planning, memory, and sensors.
  /// Subclasses add specific features (interruptions for humans, herding for animals).
  /// </summary>
  public abstract class AgentBrainBase : SerializedMonoBehaviour, IAgentBrain {
    [Inject] protected GoapPlanFactory _gPlanFactory;
    [Inject] protected GoapFeatureBankModule _goalsBankModule;

    public string[] availableFeatures;

    [Required] [SerializeField] protected AgentMemory _memory = new();
    [Required] [SerializeField] protected MemoryConsolidationModule _memoryConsolidation = new();

    [Header("Sensors")] [SerializeField] protected InteractionSensor _interactSensor;
    [SerializeField] protected VisionSensor _visionSensor;

    [Header("Navigation")] [SerializeField]
    protected AgentStuckDetector _stuckDetector = new();

    [FoldoutGroup("Debug")] [ReadOnly] public HashSet<AgentAction> actions;
    [FoldoutGroup("Debug")] [ReadOnly] public HashSet<GoalTemplate> goalTemplates;
    [FoldoutGroup("Debug")] [ReadOnly] protected ActionPlan _actionPlan;
    [FoldoutGroup("Debug")] [ReadOnly] protected AgentAction _currentAction;
    [FoldoutGroup("Debug")] [ReadOnly] protected AgentGoal _currentGoal;

    [FoldoutGroup("Debug")] [SerializeField]
    protected ActionHistoryTracker _actionHistory = new();

    [FoldoutGroup("Debug")] [SerializeField]
    protected PlanLoopDetector _planLoopDetector = new();

    [FoldoutGroup("Debug")] [SerializeField]
    protected bool _debugPlanning;

    public bool debugPlanning => _debugPlanning;

    public Dictionary<string, AgentBelief> beliefs { get; protected set; }

    protected IGoapAgentCore _agent;
    protected IGoapPlanner _gPlanner;
    protected AgentGoal _lastGoal;
    protected bool _initialized;
    protected float _currentDeltaTime;

    // IAgentBrain
    HashSet<AgentAction> IAgentBrain.actions => actions;
    HashSet<GoalTemplate> IAgentBrain.goalTemplates => goalTemplates;
    public AgentMemory memory => _memory;
    public AgentGoal currentGoal => _currentGoal;
    public ActionPlan actionPlan => _actionPlan;
    public InteractionSensor interactSensor => _interactSensor;
    public VisionSensor visionSensor => _visionSensor;
    public ActionHistoryTracker actionHistory => _actionHistory;

    public virtual void Initialize(IGoapAgentCore agent) {
      _agent = agent;
      _gPlanner = _gPlanFactory?.CreatePlanner();

      SetupBeliefs(_goalsBankModule.GetBeliefs(availableFeatures));
      SetupActions(_goalsBankModule.GetActions(agent, availableFeatures));
      SetupGoals(_goalsBankModule.GetGoals(availableFeatures));
      SetupStats();

      _memory.Initialize(new Bounds(Vector3.zero, Vector3.one * 300));
      _stuckDetector.Initialize(agent.position);
      _stuckDetector.OnStuck += HandleStuck;

      _initialized = true;
    }

    protected virtual void OnDestroy() {
      if (_stuckDetector != null) {
        _stuckDetector.OnStuck -= HandleStuck;
      }
    }

    protected virtual void OnEnable() {
      if (_interactSensor != null) {
        _interactSensor.OnActorEntered += HandleInteractionSensor;
        _interactSensor.OnActorExited += HandleInteractionSensor;
      }

      if (_visionSensor != null) {
        _visionSensor.OnActorEntered += HandleVisibilityStart;
        _visionSensor.OnActorExited += HandleVisibilityEnd;
      }
    }

    protected virtual void OnDisable() {
      if (_interactSensor != null) {
        _interactSensor.OnActorEntered -= HandleInteractionSensor;
        _interactSensor.OnActorExited -= HandleInteractionSensor;
      }

      if (_visionSensor != null) {
        _visionSensor.OnActorEntered -= HandleVisibilityStart;
        _visionSensor.OnActorExited -= HandleVisibilityEnd;
      }
    }

    public virtual void Tick(float deltaTime) {
      if (!_initialized) return;
      _currentDeltaTime = deltaTime;

      ExecuteStuckDetection(deltaTime);
      ExecutePlanning();
      ExecuteMemory(deltaTime);
      ExecuteSensors(deltaTime);
      _planLoopDetector.PurgeExpiredBlocks();
    }

    // ═══════════════════════════════════════════════════════════════
    // SETUP
    // ═══════════════════════════════════════════════════════════════

    protected void SetupBeliefs(List<AgentBelief> agentBeliefs) {
      beliefs = new Dictionary<string, AgentBelief>();
      foreach (var agentBelief in agentBeliefs) {
        beliefs.Add(agentBelief.name, agentBelief);
      }
    }

    protected void SetupGoals(List<GoalTemplate> array) {
      goalTemplates = new HashSet<GoalTemplate>(array);
    }

    protected void SetupActions(List<AgentAction> array) {
      actions = new HashSet<AgentAction>(array);
      Debug.Log($"[Brain] Actions created: {actions.Count}", this);
    }

    protected void SetupStats() {
      foreach (var agentStat in _agent.defaultStatSet.defaultPerTickDelta) {
        _agent.body.AdjustStatPerTickDelta(agentStat.Key, agentStat.Value);
      }
    }

    public AgentBelief Get(string beliefName) {
      return beliefs.GetValueOrDefault(beliefName);
    }

    // ═══════════════════════════════════════════════════════════════
    // STUCK DETECTION
    // ═══════════════════════════════════════════════════════════════

    protected void ExecuteStuckDetection(float deltaTime) {
      if (_currentAction == null) return;
      if (!_agent.navMeshAgent.hasPath) return;

      _stuckDetector.Update(_agent.navMeshAgent, deltaTime);
    }

    protected virtual void HandleStuck() {
      Debug.LogWarning($"[Brain] Agent stuck! Clearing plan. Action: {_currentAction?.name}", this);

      OnPlanFailed();
      _currentAction?.OnStop();
      ClearPlan(rememberGoal: false);

      _agent.navMeshAgent.ResetPath();
      _agent.navMeshAgent.isStopped = true;

      _stuckDetector.StartCooldown();
    }

    // ═══════════════════════════════════════════════════════════════
    // SENSORS
    // ═══════════════════════════════════════════════════════════════

    protected void ExecuteSensors(float deltaTime) {
      _visionSensor?.OnUpdate();
      _interactSensor?.OnUpdate();
    }

    protected void ExecuteMemory(float deltaTime) {
      _memoryConsolidation.Tick(memory, deltaTime, _agent.position);
      _memory.PurgeExpired();
    }

    protected virtual void HandleVisibilityEnd(ActorDescription actor) {
    }

    protected virtual void HandleVisibilityStart(ActorDescription visibleActor) {
      TryRemember(visibleActor, out var _);
    }

    protected virtual void HandleInteractionSensor(ActorDescription actor) {
    }

    public void TryRemember(ActorDescription visibleActor, out MemorySnapshot unknown) {
      unknown = null;
      var snapshot = MemorySnapshotBuilder.Create()
        .WithCreationTime(DateTime.Now)
        .With(visibleActor.descriptionData)
        .WithOptionalTarget(visibleActor.gameObject)
        .WithConfidence(Random.Range(0.5f, 1f))
        .WithLifetime(visibleActor.descriptionData.rememberDuration + 30 * Random.value)
        .WithLocation(visibleActor.transform.position)
        .Build();

      var result = _memory.TryRemember(snapshot);
      if (result == AgentMemory.RememberResult.UpdatedMemory)
        _memoryConsolidation.ReinforceMemory(snapshot);
      if (result != AgentMemory.RememberResult.Failed)
        unknown = snapshot;
    }

    // ═══════════════════════════════════════════════════════════════
    // PLANNING
    // ═══════════════════════════════════════════════════════════════

    protected virtual void ExecutePlanning() {
      var processed = false;
      processed |= TryPickNextPlannedAction();
      processed |= ExecuteCurrentAction();
      if (processed) return;

      if ((_actionPlan == null || _actionPlan.actions.Count == 0) && _currentAction == null) {
        if (TryResumePlan()) return;
        _actionPlan = CalculatePlan();
      }
    }

    /// <summary>
    /// Override in subclasses to support plan stack resume.
    /// </summary>
    protected virtual bool TryResumePlan() => false;

    protected bool TryPickNextPlannedAction() {
      if (_actionPlan == null || _actionPlan.actions.Count <= 0) return false;
      if (_currentAction != null) return true;

      _agent.navMeshAgent.ResetPath();

      _currentGoal = _actionPlan.agentGoal;
      _currentAction = _actionPlan.actions.Pop();
      _currentAction.agent = _agent;

      var allPreconditionsMet = _currentAction.AreAllPreconditionsMet(_agent);
      if (allPreconditionsMet) {
        _currentAction.OnStart();
        _stuckDetector.OnNewAction(_agent.position);
      }
      else {
        Debug.Log($"[Brain] {_currentAction.name} Preconditions not met, clearing plan", this);
        OnPlanFailed();
        ClearPlan(rememberGoal: true);
        return false;
      }

      return true;
    }

    protected virtual ActionPlan CalculatePlan() {
      if (_gPlanner == null) {
        Debug.LogError("[Brain] Planner is null, cannot calculate plan", this);
        return null;
      }

      var currentCommitment = _actionPlan?.commitment ?? 0f;
      var priorityLevel = (_currentGoal?.Priority ?? 0) + Mathf.InverseLerp(0, 1.5f, currentCommitment);

      var availableGoals = goalTemplates.Select(gt => gt.Get(_agent)).ToHashSet();
      var goalsToCheck = availableGoals;

      if (_currentGoal != null) {
        goalsToCheck = new HashSet<AgentGoal>(availableGoals.Where(g => g.isUrgent || g.Priority > priorityLevel));
      }

      // Filter out goals that are blocked due to repeated plan failures
      goalsToCheck.RemoveWhere(g => _planLoopDetector.IsGoalBlocked(g.Name));

      return _gPlanner.Plan(_agent, goalsToCheck, _lastGoal);
    }

    protected bool ExecuteCurrentAction() {
      if (_actionPlan == null || _currentAction == null) return false;

      _currentAction.OnUpdate(_currentDeltaTime);
      if (_currentAction.complete) {
        OnCurrentActionComplete();
        return true;
      }

      return true;
    }

    protected void OnCurrentActionComplete() {
      _currentAction.OnStop();
      _actionHistory.OnActionCompleted(_currentAction.name);
      _currentAction = null;
      _actionPlan?.MarkActionComplete();

      var allActionsComplete = _actionPlan.actions.Count == 0;
      if (allActionsComplete) {
        Debug.Log($"[Brain] Plan {_actionPlan.agentGoal.Name} complete", this);
        OnPlanSucceeded();
        ClearPlan(rememberGoal: true);
      }
    }

    protected void OnPlanFailed() {
      if (_actionPlan == null) return;

      var signature = _planLoopDetector.GetPlanSignature(
        _actionPlan.agentGoal.Name,
        _actionPlan.GetAllActionNames());

      _planLoopDetector.OnPlanFailed(signature);
    }

    protected void OnPlanSucceeded() {
      if (_actionPlan == null) return;

      var signature = _planLoopDetector.GetPlanSignature(
        _actionPlan.agentGoal.Name,
        _actionPlan.GetAllActionNames());

      _planLoopDetector.OnPlanSucceeded(signature);
    }

    protected void ClearPlan(bool rememberGoal) {
      if (rememberGoal && _currentGoal != null) {
        _lastGoal = _currentGoal;
      }

      _currentAction = null;
      _currentGoal = null;
      _actionPlan = null;
      OnPlanCleared();
    }

    /// <summary>
    /// Called after plan is cleared. Override for agent-specific cleanup.
    /// </summary>
    protected abstract void OnPlanCleared();
  }
}