using System.Collections.Generic;
using System.Linq;
using _Content.Scripts.AI.GOAP.Core;
using _Content.Scripts.AI.GOAP.Strategies;
using DependencyInjection;
using Genetics.Animation;
using ImprovedTimers;
using UnityEngine;
using UnityEngine.AI;

namespace _Content.Scripts.AI.GOAP.Agent {
  [RequireComponent(typeof(NavMeshAgent))]
  [RequireComponent(typeof(AnimationController))]
  public class GoapAgent : MonoBehaviour {
    [Header("Sensors")] [SerializeField] private Sensor _chaseSensor;
    [SerializeField] private Sensor _attackSensor;

    [Header("Known Locations")] [SerializeField]
    private Transform _restingPosition;

    [SerializeField] private Transform _foodShack;
    [SerializeField] private Transform _doorOnePosition;
    [SerializeField] private Transform _doorTwoPosition;

    [Header("Stats")] public float health = 100;
    public float stamina = 100;
    private AnimationController _animations;
    private Vector3 _destination;

    [Inject] private GoapFactory _gFactory;
    private IGoapPlanner _gPlanner;

    private AgentGoal _lastGoal;

    private NavMeshAgent _navMeshAgent;
    private Rigidbody _rb;

    private CountdownTimer _statsTimer;

    private GameObject _target;
    public ActionPlan actionPlan;
    public HashSet<AgentAction> actions;

    public Dictionary<string, AgentBelief> beliefs;
    public AgentAction currentAction;
    public AgentGoal currentGoal;
    public HashSet<AgentGoal> goals;

    private void Awake() {
      _navMeshAgent = GetComponent<NavMeshAgent>();
      _animations = GetComponent<AnimationController>();
      _rb = GetComponent<Rigidbody>();
      _rb.freezeRotation = true;

      _gPlanner = _gFactory.CreatePlanner();
    }

    private void Start() {
      SetupTimers();
      SetupBeliefs();
      SetupActions();
      SetupGoals();
    }

    private void Update() {
      _statsTimer.Tick();
      _animations.SetSpeed(_navMeshAgent.velocity.magnitude);

      // Update the plan and current action if there is one
      if (currentAction == null) {
        Debug.Log("Calculating any potential new plan");
        CalculatePlan();

        if (actionPlan != null && actionPlan.Actions.Count > 0) {
          _navMeshAgent.ResetPath();

          currentGoal = actionPlan.AgentGoal;
          Debug.Log($"Goal: {currentGoal.Name} with {actionPlan.Actions.Count} actions in plan");
          currentAction = actionPlan.Actions.Pop();
          Debug.Log($"Popped action: {currentAction.Name}");
          // Verify all precondition effects are true
          if (currentAction.Preconditions.All(b => b.Evaluate())) {
            currentAction.Start();
          }
          else {
            Debug.Log("Preconditions not met, clearing current action and goal");
            currentAction = null;
            currentGoal = null;
          }
        }
      }

      // If we have a current action, execute it
      if (actionPlan != null && currentAction != null) {
        currentAction.Update(Time.deltaTime);

        if (currentAction.Complete) {
          Debug.Log($"{currentAction.Name} complete");
          currentAction.Stop();
          currentAction = null;

          if (actionPlan.Actions.Count == 0) {
            Debug.Log("Plan complete");
            _lastGoal = currentGoal;
            currentGoal = null;
          }
        }
      }
    }

    private void OnEnable() {
      _chaseSensor.OnTargetChanged += HandleTargetChanged;
    }

    private void OnDisable() {
      _chaseSensor.OnTargetChanged -= HandleTargetChanged;
    }

    private void SetupBeliefs() {
      beliefs = new Dictionary<string, AgentBelief>();
      var factory = new BeliefFactory(this, beliefs);

      factory.AddBelief("Nothing", () => false);

      factory.AddBelief("AgentIdle", () => !_navMeshAgent.hasPath);
      factory.AddBelief("AgentMoving", () => _navMeshAgent.hasPath);
      factory.AddBelief("AgentHealthLow", () => health < 30);
      factory.AddBelief("AgentIsHealthy", () => health >= 50);
      factory.AddBelief("AgentStaminaLow", () => stamina < 10);
      factory.AddBelief("AgentIsRested", () => stamina >= 50);

      factory.AddLocationBelief("AgentAtDoorOne", 3f, _doorOnePosition);
      factory.AddLocationBelief("AgentAtDoorTwo", 3f, _doorTwoPosition);
      factory.AddLocationBelief("AgentAtRestingPosition", 3f, _restingPosition);
      factory.AddLocationBelief("AgentAtFoodShack", 3f, _foodShack);

      factory.AddSensorBelief("PlayerInChaseRange", _chaseSensor);
      factory.AddSensorBelief("PlayerInAttackRange", _attackSensor);
      factory.AddBelief("AttackingPlayer", () => false); // Player can always be attacked, this will never become true
    }

    private void SetupActions() {
      actions = new HashSet<AgentAction>();

      actions.Add(new AgentAction.Builder("Relax")
        .WithStrategy(new IdleStrategy(5))
        .AddEffect(beliefs["Nothing"])
        .Build());

      actions.Add(new AgentAction.Builder("Wander Around")
        .WithStrategy(new WanderStrategy(_navMeshAgent, 10))
        .AddEffect(beliefs["AgentMoving"])
        .Build());

      actions.Add(new AgentAction.Builder("MoveToEatingPosition")
        .WithStrategy(new MoveStrategy(_navMeshAgent, () => _foodShack.position))
        .AddEffect(beliefs["AgentAtFoodShack"])
        .Build());

      actions.Add(new AgentAction.Builder("Eat")
        .WithStrategy(new IdleStrategy(5)) // Later replace with a Command
        .AddPrecondition(beliefs["AgentAtFoodShack"])
        .AddEffect(beliefs["AgentIsHealthy"])
        .Build());

      actions.Add(new AgentAction.Builder("MoveToDoorOne")
        .WithStrategy(new MoveStrategy(_navMeshAgent, () => _doorOnePosition.position))
        .AddEffect(beliefs["AgentAtDoorOne"])
        .Build());

      actions.Add(new AgentAction.Builder("MoveToDoorTwo")
        .WithStrategy(new MoveStrategy(_navMeshAgent, () => _doorTwoPosition.position))
        .AddEffect(beliefs["AgentAtDoorTwo"])
        .Build());

      actions.Add(new AgentAction.Builder("MoveFromDoorOneToRestArea")
        .WithCost(2)
        .WithStrategy(new MoveStrategy(_navMeshAgent, () => _restingPosition.position))
        .AddPrecondition(beliefs["AgentAtDoorOne"])
        .AddEffect(beliefs["AgentAtRestingPosition"])
        .Build());

      actions.Add(new AgentAction.Builder("MoveFromDoorTwoRestArea")
        .WithStrategy(new MoveStrategy(_navMeshAgent, () => _restingPosition.position))
        .AddPrecondition(beliefs["AgentAtDoorTwo"])
        .AddEffect(beliefs["AgentAtRestingPosition"])
        .Build());

      actions.Add(new AgentAction.Builder("Rest")
        .WithStrategy(new IdleStrategy(5))
        .AddPrecondition(beliefs["AgentAtRestingPosition"])
        .AddEffect(beliefs["AgentIsRested"])
        .Build());

      actions.Add(new AgentAction.Builder("ChasePlayer")
        .WithStrategy(new MoveStrategy(_navMeshAgent, () => beliefs["PlayerInChaseRange"].Location))
        .AddPrecondition(beliefs["PlayerInChaseRange"])
        .AddEffect(beliefs["PlayerInAttackRange"])
        .Build());

      actions.Add(new AgentAction.Builder("AttackPlayer")
        .WithStrategy(new AttackStrategy(_animations))
        .AddPrecondition(beliefs["PlayerInAttackRange"])
        .AddEffect(beliefs["AttackingPlayer"])
        .Build());
    }

    private void SetupGoals() {
      goals = new HashSet<AgentGoal>();

      goals.Add(new AgentGoal.Builder("Chill Out")
        .WithPriority(1)
        .WithDesiredEffect(beliefs["Nothing"])
        .Build());

      goals.Add(new AgentGoal.Builder("Wander")
        .WithPriority(1)
        .WithDesiredEffect(beliefs["AgentMoving"])
        .Build());

      goals.Add(new AgentGoal.Builder("KeepHealthUp")
        .WithPriority(2)
        .WithDesiredEffect(beliefs["AgentIsHealthy"])
        .Build());

      goals.Add(new AgentGoal.Builder("KeepStaminaUp")
        .WithPriority(2)
        .WithDesiredEffect(beliefs["AgentIsRested"])
        .Build());

      goals.Add(new AgentGoal.Builder("SeekAndDestroy")
        .WithPriority(3)
        .WithDesiredEffect(beliefs["AttackingPlayer"])
        .Build());
    }

    private void SetupTimers() {
      _statsTimer = new CountdownTimer(2f);
      _statsTimer.OnTimerStop += () => {
        UpdateStats();
        _statsTimer.Start();
      };
      _statsTimer.Start();
    }

    // TODO move to stats system
    private void UpdateStats() {
      stamina += InRangeOf(_restingPosition.position, 3f) ? 20 : -10;
      health += InRangeOf(_foodShack.position, 3f) ? 20 : -5;
      stamina = Mathf.Clamp(stamina, 0, 100);
      health = Mathf.Clamp(health, 0, 100);
    }

    private bool InRangeOf(Vector3 pos, float range) {
      return Vector3.Distance(transform.position, pos) < range;
    }

    private void HandleTargetChanged() {
      Debug.Log("Target changed, clearing current action and goal");
      // Force the planner to re-evaluate the plan
      currentAction = null;
      currentGoal = null;
    }

    private void CalculatePlan() {
      var priorityLevel = currentGoal?.Priority ?? 0;

      var goalsToCheck = goals;

      // If we have a current goal, we only want to check goals with higher priority
      if (currentGoal != null) {
        Debug.Log("Current goal exists, checking goals with higher priority");
        goalsToCheck = new HashSet<AgentGoal>(goals.Where(g => g.Priority > priorityLevel));
      }

      var potentialPlan = _gPlanner.Plan(this, goalsToCheck, _lastGoal);
      if (potentialPlan != null) actionPlan = potentialPlan;
    }
  }
}