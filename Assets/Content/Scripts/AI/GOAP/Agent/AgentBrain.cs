using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Core;
using Content.Scripts.AI.GOAP.Planning;
using Content.Scripts.AI.GOAP.Strategies;
using Reflex.Attributes;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.GOAP.Agent {
  public class AgentBody : SerializedMonoBehaviour {
    private IGoapAgent _agent;

    public void Initialize(IGoapAgent agent) {
      _agent = agent;
    }
  }
  
  [RequireComponent(typeof(BrainBeliefsController))]
  public class AgentBrain : SerializedMonoBehaviour {
    [Header("Sensors")] [VerticalGroup("Sensors")] [SerializeField]
    private Sensor _chaseSensor;

    [VerticalGroup("Sensors")] [SerializeField]
    private Sensor _attackSensor;

    public BrainBeliefsController beliefsController;

    [Inject] private GoapFactory _gFactory;
    [Inject] private GoalsBankModule _goalsBankModule;
    private IGoapPlanner _gPlanner;
    private AgentGoal _lastGoal;

    [FoldoutGroup("Debug")] [ReadOnly] public HashSet<AgentAction> actions;
    [FoldoutGroup("Debug")] [ReadOnly] public HashSet<AgentGoal> goals;

    [Space] [FoldoutGroup("Debug")] [ReadOnly]
    private ActionPlan _actionPlan;

    [FoldoutGroup("Debug")] [ReadOnly] private AgentAction _currentAction;
    [FoldoutGroup("Debug")] [ReadOnly] private AgentGoal _currentGoal;

    private IGoapAgent _agent;
    public Sensor chaseSensor => _chaseSensor;
    public Sensor attackSensor => _attackSensor;

    private void Update() {
      var processed = false;
      processed |= TryPickNextPlannedAction();
      processed |= ExecutePlan();
      if (processed) return;

      if (_currentAction == null || _actionPlan == null) {
        Debug.LogError("lost a plan.", this);
        CalculatePlan();
      }
    }

    private bool TryPickNextPlannedAction() {
      // Update the plan and current action if there is one
      if (_currentAction != null) return false;
      if (_actionPlan == null || _actionPlan.Actions.Count <= 0) return false;

      _agent.navMeshAgent.ResetPath();

      _currentGoal = _actionPlan.AgentGoal;
      Debug.Log($"Goal: {_currentGoal.Name} with {_actionPlan.Actions.Count} actions in plan", this);
      _currentAction = _actionPlan.Actions.Pop();
      Debug.Log($"Popped action: {_currentAction.Name}", this);

      CalculatePlan();
      // Verify all precondition effects are true
      if (_currentAction.Preconditions.All(b => b.Evaluate())) {
        _currentAction.Start();
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
      _currentAction.Update(Time.deltaTime);
      if (!_currentAction.Complete) return true;

      Debug.Log($"{_currentAction.Name} complete", this);
      _currentAction.Stop();
      _currentAction = null;

      if (_actionPlan.Actions.Count != 0) return true;
      Debug.Log("Plan complete", this);
      _lastGoal = _currentGoal;
      _currentGoal = null;

      return true;
    }


    public void Initialize(IGoapAgent agent) {
      _agent = agent;
      _gPlanner = _gFactory?.CreatePlanner();
      beliefsController.SetupBeliefs(_agent);
      SetupActions();
      SetupGoals();
    }


    private void OnEnable() {
      _chaseSensor.OnTargetChanged += HandleTargetChanged;
    }

    private void OnDisable() {
      _chaseSensor.OnTargetChanged -= HandleTargetChanged;
    }

    [VerticalGroup("Sensors")]
    [Button]
    [EnableIf("@_chaseSensor == null || _attackSensor == null")]
    private void CreateSensors() {
      if (_chaseSensor == null) _chaseSensor = SpawnSensor("ChaseSensor");
      if (_attackSensor == null) _attackSensor = SpawnSensor("AttackSensor");
    }

    private Sensor SpawnSensor(string name) {
      var sensor = new GameObject(name).AddComponent<Sensor>();
      sensor.transform.SetParent(transform);
      sensor.transform.localPosition = Vector3.zero;
      return sensor;
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
      actions.Add(new AgentAction.Builder("Relax")
        .WithStrategy(new IdleStrategy(5))
        .AddEffect(beliefsController.Get("Nothing"))
        .Build());

      actions.Add(new AgentAction.Builder("Wander Around")
        .WithStrategy(new WanderStrategy(_agent.navMeshAgent, 10))
        .AddEffect(beliefsController.Get("AgentMoving"))
        .Build());

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

      actions.Add(new AgentAction.Builder("Rest")
        .WithStrategy(new IdleStrategy(5))
        .AddPrecondition(beliefsController.Get("AgentAtRestingPosition"))
        .AddEffect(beliefsController.Get("AgentIsRested"))
        .Build());

      actions.Add(new AgentAction.Builder("ChasePlayer")
        .WithStrategy(new MoveStrategy(_agent.navMeshAgent, () => beliefsController.Get("PlayerInChaseRange").Location))
        .AddPrecondition(beliefsController.Get("PlayerInChaseRange"))
        .AddEffect(beliefsController.Get("PlayerInAttackRange"))
        .Build());

      actions.Add(new AgentAction.Builder("AttackPlayer")
        .WithStrategy(new AttackStrategy(_agent.animationController))
        .AddPrecondition(beliefsController.Get("PlayerInAttackRange"))
        .AddEffect(beliefsController.Get("AttackingPlayer"))
        .Build());
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
        Debug.Log("Current goal exists, checking goals with higher priority");
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