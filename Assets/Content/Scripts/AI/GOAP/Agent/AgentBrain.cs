using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent.Descriptors;
using Content.Scripts.AI.GOAP.Beliefs;
using Content.Scripts.AI.GOAP.Goals;
using Content.Scripts.AI.GOAP.Planning;
using Content.Scripts.AI.GOAP.Stats;
using Content.Scripts.AI.GOAP.Strategies;
using Reflex.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Content.Scripts.AI.GOAP.Agent {
  [RequireComponent(typeof(BrainBeliefsController))]
  public class AgentBrain : SerializedMonoBehaviour {
    [Inject] private GoapPlanFactory _gPlanFactory;
    [Inject] private GoalsBankModule _goalsBankModule;

    [Required] [SerializeField] private AgentMemory _memory = new AgentMemory();
    [SerializeField] private List<ActionDataSO> _defaultActions = new();

    [Header("Sensors")] [VerticalGroup("Sensors")] [SerializeField]
    private SimpleSensor _chaseSensor;

    [VerticalGroup("Sensors")] [SerializeField]
    private VisionSensor _visionSensor;

    [Required] public BrainBeliefsController beliefsController;

    [FoldoutGroup("Debug")] [ReadOnly] public HashSet<AgentAction> actions;
    [FoldoutGroup("Debug")] [ReadOnly] public HashSet<AgentGoal> goals;

    [Space] [FoldoutGroup("Debug")] [ReadOnly]
    private ActionPlan _actionPlan;

    [FoldoutGroup("Debug")] [ReadOnly] private AgentAction _currentAction;
    [FoldoutGroup("Debug")] [ReadOnly] private AgentGoal _currentGoal;

    private IGoapAgent _agent;
    public SimpleSensor chaseSensor => _chaseSensor;
    public VisionSensor visionSensor => _visionSensor;

    public AgentMemory memory => _memory;

    private IGoapPlanner _gPlanner;
    private AgentGoal _lastGoal;


    public void Initialize(IGoapAgent agent) {
      _agent = agent;
      _gPlanner = _gPlanFactory?.CreatePlanner();
      beliefsController.SetupBeliefs(_agent);
      SetupActions();
      SetupGoals();
      SetupStats();
    }

    private void SetupStats() {
      foreach (var agentStat in _agent.defaultStatSet.defaultPerTickDelta) {
        _agent.body.AdjustStatPerTickDelta(agentStat.Key, agentStat.Value);
      }
    }

    private void OnEnable() {
      _chaseSensor.OnTargetChanged += HandleTargetChanged;
      _visionSensor.OnActorEntered += HandleVisibilityStart;
      _visionSensor.OnActorExited += HandleVisibilityEnd;
    }

    private void OnDisable() {
      _chaseSensor.OnTargetChanged -= HandleTargetChanged;
      _visionSensor.OnActorEntered -= HandleVisibilityStart;
      _visionSensor.OnActorExited -= HandleVisibilityEnd;
    }

    public void Tick(float deltaTime) {
      ExecutePlanning();
      ExecuteMemory(deltaTime);
    }

    private void ExecuteMemory(float deltaTime) {
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
        .WithLifetime(5 + 40 * Random.value)
        .WithLocation(visibleActor.transform.position)
        .Build();
      var result = _memory.Remember(snapshot);
    }

    private void ExecutePlanning() {
      var processed = false;
      processed |= TryPickNextPlannedAction();
      processed |= ExecutePlan();
      if (processed) return;

      if (_currentAction == null && (_actionPlan == null || _actionPlan.Actions.Count == 0)) {
        CalculatePlan();
      }
    }

    private bool TryPickNextPlannedAction() {
      // Update the plan and current action if there is one
      if (_actionPlan == null || _actionPlan.Actions.Count <= 0) return false;
      if (_currentAction != null) return true;

      _agent.navMeshAgent.ResetPath();

      _currentGoal = _actionPlan.AgentGoal;
      _currentAction = _actionPlan.Actions.Pop();
      _currentAction.agent = _agent;

      CalculatePlan();
      // Verify all precondition effects are true
      var allPreconditionsMet = _currentAction.AreAllPreconditionsMet(_agent);
      if (allPreconditionsMet) {
        _currentAction.OnStart();
      }
      else {
        Debug.Log("Preconditions not met, clearing current action and goal", this);
        _currentAction = null;
        _currentGoal = null;
      }

      return true;
    }

    private bool ExecutePlan() {
      // If we have a current action, execute it
      if (_actionPlan == null || _currentAction == null) return false;
      _currentAction.OnUpdate(Time.deltaTime);
      if (!_currentAction.complete) return true;

      // Debug.Log($"{_currentAction.Name} complete", this);
      _currentAction.OnStop();
      _currentAction = null;

      if (_actionPlan.Actions.Count != 0) return true;
      // Debug.Log("Plan complete", this);
      _lastGoal = _currentGoal;
      _currentGoal = null;

      return true;
    }


    private void SetupGoals() {
      goals = new HashSet<AgentGoal>();
      var defaultGoals = _goalsBankModule.GetAgentDefaultGoals(_agent);
      foreach (var defaultGoal in defaultGoals) {
        goals.Add(defaultGoal);
      }
    }

    private void SetupActions() {
      actions = new HashSet<AgentAction>();

      foreach (var actionData in _defaultActions) {
        var action = actionData.GetAction(_agent);
        actions.Add(action);
      }

      Debug.Log($"Action created: {actions.Count}", this);

      // actions.Add(new AgentAction.Builder("Relax")
      //   .WithStrategy(new IdleStrategy(5))
      //   .AddEffect(beliefsController.Get(AgentConstants.Nothing))
      //   .WithCost(1)
      //   .Build());

      // actions.Add(new AgentAction.Builder("Wander Around")
      //   .WithStrategy(new WanderStrategy(_agent, () => Random.value * 20f + 5f, Vector3.zero))
      //   .AddEffect(beliefsController.Get(AgentConstants.Moving))
      //   .WithCost(1)
      //   .Build());
      //
      // actions.Add(new AgentAction.Builder("MoveToNearestFood")
      //   .WithStrategy(
      //     new MoveStrategy()
      //       .SetAgent(_agent)
      //       .SetDestination(() => memory.GetNearest(_agent.position, new[] { "FOOD" }, ms => ms.target != null))
      //   )
      //   .WithCost(2)
      //   .AddPrecondition(beliefsController.Get("RemembersFoodNearby"))
      //   .AddEffect(beliefsController.Get("AgentAtFood"))
      //   .Build());

      // actions.Add(new AgentAction.Builder("Eat")
      //   .AddPrecondition(beliefsController.Get("AgentAtFood"))
      //   //.AddPrecondition(beliefsController.Get("AgentSeeFood"))
      //   .WithStrategy(new EatNearestStrategy(_agent))
      //   .WithCost(1)
      //   .AddEffect(beliefsController.Get("AgentIsNotHungry"))
      //   .Build());

      // actions.Add(new AgentAction.Builder("MoveToEatingPosition")
      //   .WithStrategy(new MoveStrategy(_agent.navMeshAgent, () => _foodShack.position))
      //   .AddEffect(beliefs["AgentAtFoodShack"])
      //   .Build());

      // actions.Add(new AgentAction.Builder("Eat")
      //   .WithStrategy(new IdleStrategy(5)) // Later replace with a Command
      //   .AddPrecondition(beliefs.Get("AgentAtFoodShack"))
      //   .AddEffect(beliefs.Get("AgentIsHealthy"))
      // .Build());

      // actions.Add(new AgentAction.Builder("MoveToDoorOne")
      //   .WithStrategy(new MoveStrategy(_agent.navMeshAgent, () => _doorOnePosition.position))
      //   .AddEffect(beliefs["AgentAtDoorOne"])
      //   .Build());
      //
      // actions.Add(new AgentAction.Builder("MoveToDoorTwo")
      //   .WithStrategy(new MoveStrategy(_agent.navMeshAgent, () => _doorTwoPosition.position))
      //   .AddEffect(beliefs["AgentAtDoorTwo"])
      //   .Build());
      //
      // actions.Add(new AgentAction.Builder("MoveFromDoorOneToRestArea")
      //   .WithCost(2)
      //   .WithStrategy(new MoveStrategy(_agent.navMeshAgent, () => _restingPosition.position))
      //   .AddPrecondition(beliefs["AgentAtDoorOne"])
      //   .AddEffect(beliefs["AgentAtRestingPosition"])
      //   .Build());
      //
      // actions.Add(new AgentAction.Builder("MoveFromDoorTwoRestArea")
      //   .WithStrategy(new MoveStrategy(_agent.navMeshAgent, () => _restingPosition.position))
      //   .AddPrecondition(beliefs["AgentAtDoorTwo"])
      //   .AddEffect(beliefs["AgentAtRestingPosition"])
      //   .Build());

      // actions.Add(new AgentAction.Builder("ChasePlayer")
      //   .WithStrategy(new MoveStrategy(_agent.navMeshAgent, () => beliefsController.Get("PlayerInChaseRange").Location))
      //   .AddPrecondition(beliefsController.Get("PlayerInChaseRange"))
      //   .AddEffect(beliefsController.Get("PlayerInAttackRange"))
      //   .Build());
      //
      // actions.Add(new AgentAction.Builder("AttackPlayer")
      //   .WithStrategy(new AttackStrategy(_agent.animationController))
      //   .AddPrecondition(beliefsController.Get("PlayerInAttackRange"))
      //   .AddEffect(beliefsController.Get("AttackingPlayer"))
      //   .Build());
    }


    private void HandleTargetChanged() {
      Debug.Log("Target changed, clearing current action and goal");
      // Force the planner to re-evaluate the plan
      _currentAction = null;
      _currentGoal = null;
    }

    private void CalculatePlan() {
      var priorityLevel = _currentGoal?.Priority ?? 0;
      var goalsToCheck = goals;

      // If we have a current goal, we only want to check goals with higher priority
      if (_currentGoal != null) {
        // Debug.Log("Current goal exists, checking goals with higher priority");
        goalsToCheck = new HashSet<AgentGoal>(goals.Where(g => g.Priority > priorityLevel));
      }

      if (_gPlanner == null) {
        Debug.LogError("Planner is null, cannot calculate plan", this);
        return;
      }

      var potentialPlan = _gPlanner?.Plan(_agent, goalsToCheck, _lastGoal);
      if (potentialPlan != null) _actionPlan = potentialPlan;
    }
  }
}