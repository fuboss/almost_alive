using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent.Memory;
using Content.Scripts.AI.GOAP.Agent.Sensors;
using Content.Scripts.AI.GOAP.Beliefs;
using Content.Scripts.AI.GOAP.Goals;
using Content.Scripts.AI.GOAP.Planning;
using Content.Scripts.Game;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;
using Random = UnityEngine.Random;

namespace Content.Scripts.AI.GOAP.Agent {
  public class AgentBrain : SerializedMonoBehaviour {
    [Inject] private GoapPlanFactory _gPlanFactory;
    [Inject] private GoatFeatureBankModule _goalsBankModule;

    public string[] availableFeatures = {
      "Common_FeatureSet",
      "Hunger_FeatureSet",
    };

    [Required] [SerializeField] private AgentMemory _memory = new();
    [Required] [SerializeField] private MemoryConsolidationModule _memoryConsolidation = new();

    [Header("Sensors")] [VerticalGroup("Sensors")] [SerializeField]
    private InteractionSensor _interactSensor;

    [VerticalGroup("Sensors")] [SerializeField]
    private VisionSensor _visionSensor;

    [FoldoutGroup("Debug")] [ReadOnly] public HashSet<AgentAction> actions;
    [FoldoutGroup("Debug")] [ReadOnly] public HashSet<GoalTemplate> goalTemplates;

    [Space] [FoldoutGroup("Debug")] [ReadOnly]
    private ActionPlan _actionPlan;

    [FoldoutGroup("Debug")] [ReadOnly] private AgentAction _currentAction;
    [FoldoutGroup("Debug")] [SerializeField] private ActionHistoryTracker _actionHistory = new();
    [FoldoutGroup("Debug")] [ReadOnly] private AgentGoal _currentGoal;
    public Dictionary<string, AgentBelief> beliefs;
    private IGoapAgent _agent;
    public InteractionSensor interactSensor => _interactSensor;
    public ActionHistoryTracker actionHistory => _actionHistory;
    public VisionSensor visionSensor => _visionSensor;

    public AgentMemory memory => _memory;

    public ActionPlan actionPlan {
      get => _actionPlan;
      set => _actionPlan = value;
    }

    private IGoapPlanner _gPlanner;
    private AgentGoal _lastGoal;
    private bool _initialized;


    public void Initialize(IGoapAgent agent) {
      _agent = agent;
      _gPlanner = _gPlanFactory?.CreatePlanner();

      SetupBeliefs(_goalsBankModule.GetBeliefs(agent, availableFeatures));
      SetupActions(_goalsBankModule.GetActions(agent, availableFeatures));
      SetupGoals(_goalsBankModule.GetGoals(availableFeatures));
      SetupStats();

      _memory.Initialize(new Bounds(Vector3.zero, Vector3.one * 300));
      _initialized = true;
    }

    private void SetupBeliefs(List<AgentBelief> agentBeliefs) {
      beliefs = new Dictionary<string, AgentBelief>();
      foreach (var agentBelief in agentBeliefs) {
        beliefs.Add(agentBelief.name, agentBelief);
      }
    }

    public AgentBelief Get(string beliefName) {
      return beliefs.GetValueOrDefault(beliefName);
    }

    private void SetupStats() {
      foreach (var agentStat in _agent.defaultStatSet.defaultPerTickDelta) {
        _agent.body.AdjustStatPerTickDelta(agentStat.Key, agentStat.Value);
      }
    }

    private void OnEnable() {
      _interactSensor.OnActorEntered += HandleInteractionSensor;
      _interactSensor.OnActorExited += HandleInteractionSensor;
      _visionSensor.OnActorEntered += HandleVisibilityStart;
      _visionSensor.OnActorExited += HandleVisibilityEnd;
    }

    private void OnDisable() {
      _interactSensor.OnActorEntered -= HandleInteractionSensor;
      _interactSensor.OnActorExited -= HandleInteractionSensor;
      _visionSensor.OnActorEntered -= HandleVisibilityStart;
      _visionSensor.OnActorExited -= HandleVisibilityEnd;
    }

    public void Tick(float deltaTime) {
      if (!_initialized) return;
      ExecutePlanning();
      ExecuteMemory(deltaTime);
      ExecuteSensors(deltaTime);
    }

    private void ExecuteSensors(float deltaTime) {
      _visionSensor.OnUpdate();
      _interactSensor.OnUpdate();
    }

    private void ExecuteMemory(float deltaTime) {
      _memoryConsolidation.Tick(memory, deltaTime, _agent.position);
      _memory.PurgeExpired(); //todo: use cooldown
    }

    private void HandleVisibilityEnd(ActorDescription noMoreVisibleActor) {
    }

    private void HandleVisibilityStart(ActorDescription visibleActor) {
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
    }

    private void ExecutePlanning() {
      var processed = false;
      processed |= TryPickNextPlannedAction();
      processed |= ExecuteCurrentAction();
      if (processed) return;

      if ((actionPlan == null || actionPlan.actions.Count == 0) && _currentAction == null) {
        actionPlan = CalculatePlan();
      }
    }

    private bool TryPickNextPlannedAction() {
      // Update the plan and current action if there is one
      if (_actionPlan == null || _actionPlan.actions.Count <= 0) return false;
      if (_currentAction != null) return true;

      _agent.navMeshAgent.ResetPath();

      _currentGoal = actionPlan.agentGoal;
      _currentAction = actionPlan.actions.Pop();
      _currentAction.agent = _agent;

      //actionPlan = CalculatePlan();
      // Verify all precondition effects are true
      var allPreconditionsMet = _currentAction.AreAllPreconditionsMet(_agent);
      if (allPreconditionsMet) {
        _currentAction.OnStart();
      }
      else {
        Debug.Log($"[Brain] {_currentAction.name} Preconditions not met, clearing current action and goal", this);
        _lastGoal = _currentGoal;
        _currentAction = null;
        _currentGoal = null;
        _actionPlan = null;
        return false;
      }

      return true;
    }

    private ActionPlan CalculatePlan() {
      var currentCommitment = actionPlan?.commitment ?? 0f;
      var priorityLevel = (_currentGoal?.Priority ?? 0) + Mathf.InverseLerp(0, 1.5f, currentCommitment);

      var availableGoals = goalTemplates.Select(gt => gt.Get(_agent)).ToHashSet();
      var goalsToCheck = availableGoals;

      // If we have a current goal, we only want to check goals with higher priority
      if (_currentGoal != null) {
        goalsToCheck = new HashSet<AgentGoal>(availableGoals.Where(g => g.isUrgent || g.Priority > priorityLevel));
      }

      if (_gPlanner == null) {
        Debug.LogError("[Brain] Planner is null, cannot calculate plan", this);
        return null;
      }

      return _gPlanner?.Plan(_agent, goalsToCheck, _lastGoal);
    }

    private bool ExecuteCurrentAction() {
      if (actionPlan == null || _currentAction == null) return false;

      _currentAction.OnUpdate(Time.deltaTime);
      if (_currentAction.complete) {
        OnCurrentActionComplete();
        return true;
      }

      // Debug.Log($"{_currentAction.Name} complete", this);
      return true;
    }

    private void OnCurrentActionComplete() {
      _currentAction.OnStop();
      _actionHistory.OnActionCompleted(_currentAction.name);
      _currentAction = null;
      _actionPlan?.MarkActionComplete();

      var allActionsComplete = actionPlan.actions.Count == 0;
      if (allActionsComplete) {
        Debug.Log($"[Brain] Plan {actionPlan.agentGoal.Name} complete", this);
        _lastGoal = _currentGoal;
        _currentGoal = null;
        actionPlan = null;
      }
    }


    private void SetupGoals(List<GoalTemplate> array) {
      goalTemplates = new HashSet<GoalTemplate>(array);
    }

    private void SetupActions(List<AgentAction> array) {
      actions = new HashSet<AgentAction>(array);
      Debug.Log($"[Brain] Action created: {actions.Count}", this);
    }


    private void HandleInteractionSensor(ActorDescription actorDescription) {
      //Debug.Log($"[Brain] Interaction sensor triggered by {actorDescription.name}", actorDescription);
      // // Force the planner to re-evaluate the plan
      // _currentAction = null;
      // _currentGoal = null;
    }
  }
}