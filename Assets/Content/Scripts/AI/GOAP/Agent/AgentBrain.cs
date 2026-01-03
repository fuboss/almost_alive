using System;
using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent.Descriptors;
using Content.Scripts.AI.GOAP.Beliefs;
using Content.Scripts.AI.GOAP.Goals;
using Content.Scripts.AI.GOAP.Planning;
using Reflex.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;
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

    [Header("Sensors")] [VerticalGroup("Sensors")] [SerializeField]
    private InteractionSensor _interactSensor;

    [VerticalGroup("Sensors")] [SerializeField]
    private VisionSensor _visionSensor;

    [FoldoutGroup("Debug")] [ReadOnly] public HashSet<AgentAction> actions;
    [FoldoutGroup("Debug")] [ReadOnly] public HashSet<AgentGoal> goals;

    [Space] [FoldoutGroup("Debug")] [ReadOnly]
    private ActionPlan _actionPlan;

    [FoldoutGroup("Debug")] [ReadOnly] private AgentAction _currentAction;
    [FoldoutGroup("Debug")] [ReadOnly] private AgentGoal _currentGoal;
    public Dictionary<string, AgentBelief> beliefs;
    private IGoapAgent _agent;
    public InteractionSensor interactSensor => _interactSensor;
    public VisionSensor visionSensor => _visionSensor;

    public AgentMemory memory => _memory;

    public ActionPlan actionPlan {
      get => _actionPlan;
      set {
        // Debug.Log(
        //   value != null
        //     ? $"New ActionPlane set: {value.AgentGoal.Name}::{string.Join(", ", value.Actions.Select(a => a.name))}"
        //     : "NULL", this);
        _actionPlan = value;
      }
    }

    private IGoapPlanner _gPlanner;
    private AgentGoal _lastGoal;
    private bool _initialized;


    public void Initialize(IGoapAgent agent) {
      _agent = agent;
      _gPlanner = _gPlanFactory?.CreatePlanner();

      SetupBeliefs(_goalsBankModule.GetBeliefs(agent, availableFeatures));
      SetupActions(_goalsBankModule.GetActions(agent, availableFeatures));
      SetupGoals(_goalsBankModule.GetGoals(agent, availableFeatures));
      SetupStats();
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
      processed |= ExecuteCurrentAction();
      if (processed) return;

      if ((actionPlan == null || actionPlan.Actions.Count == 0) && _currentAction == null) {
        actionPlan = CalculatePlan();
      }
    }

    private bool TryPickNextPlannedAction() {
      // Update the plan and current action if there is one
      if (_actionPlan == null || _actionPlan.Actions.Count <= 0) return false;
      if (_currentAction != null) return true;

      _agent.navMeshAgent.ResetPath();

      _currentGoal = actionPlan.AgentGoal;
      _currentAction = actionPlan.Actions.Pop();
      _currentAction.agent = _agent;

      //actionPlan = CalculatePlan();
      // Verify all precondition effects are true
      var allPreconditionsMet = _currentAction.AreAllPreconditionsMet(_agent);
      if (allPreconditionsMet) {
        _currentAction.OnStart();
      }
      else {
        Debug.Log($"{_currentAction.name} Preconditions not met, clearing current action and goal", this);
        _currentAction = null;
        _currentGoal = null;
      }

      return true;
    }

    private ActionPlan CalculatePlan() {
//      Debug.Log($"Calculating new plan. CurrentGoal: {_currentGoal?.Name ?? "NONE"}", this);
      var priorityLevel = _currentGoal?.Priority ?? 0;
      var goalsToCheck = goals;

      // If we have a current goal, we only want to check goals with higher priority
      if (_currentGoal != null) {
        goalsToCheck = new HashSet<AgentGoal>(goals.Where(g => g.Priority > priorityLevel));
      }

      if (_gPlanner == null) {
        Debug.LogError("Planner is null, cannot calculate plan", this);
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
      _currentAction = null;

      var allActionsComplete = actionPlan.Actions.Count == 0;
      if (allActionsComplete) {
        Debug.Log($"Plan {actionPlan.AgentGoal.Name} complete", this);
        _lastGoal = _currentGoal;
        _currentGoal = null;
        actionPlan = null;
        
      }
    }


    private void SetupGoals(List<AgentGoal> array) {
      goals = new HashSet<AgentGoal>(array);
    }

    private void SetupActions(List<AgentAction> array) {
      actions = new HashSet<AgentAction>(array);
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


    private void HandleInteractionSensor(ActorDescription actorDescription) {
      // Debug.Log("Target changed, clearing current action and goal");
      // // Force the planner to re-evaluate the plan
      // _currentAction = null;
      // _currentGoal = null;
    }
  }
}