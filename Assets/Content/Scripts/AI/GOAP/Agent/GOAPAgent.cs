using Content.Scripts.AI.Camp;
using Content.Scripts.AI.Craft;
using Content.Scripts.AI.GOAP.Agent.Memory;
using Content.Scripts.Core.Simulation;
using Content.Scripts.Game;
using Content.Scripts.Game.Work;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using VContainer;

namespace Content.Scripts.AI.GOAP.Agent {
  [RequireComponent(typeof(NavMeshAgent))]
  public class GOAPAgent : SerializedMonoBehaviour, IGoapAgent, ISimulatable {
    [Inject] private SimulationLoop _simLoop;
    [Inject] private SimulationTimeController _simTime;
    [Inject] private RecipeModule _recipeModule;
    [Inject] private CampModule _campModule;

    [SerializeField] private AgentStatSetSO _defaultStatSet;
    [SerializeField] private AgentBrain _agentBrain;
    [SerializeField]private AgentBody _agentBody;
    [SerializeField] private ActorInventory _inventory;
    [SerializeField] private WorkPriority _workPriority;
    [SerializeField] private float _sprintSpeedModifier = 1.5f;

    [FoldoutGroup("Progression")] [SerializeField]
    private AgentExperience _experience = new();

    [FoldoutGroup("Progression")] [SerializeField]
    private AgentRecipes _recipes = new();

    [ShowInInspector, ReadOnly] private ActorDescription _transientTarget;
    [ShowInInspector, ReadOnly] private float _baseNavSpeed;
    
    public int tickPriority => 0;

    // IGoapAgentCore
    public AgentBrain agentBrain => _agentBrain;
    public NavMeshAgent navMeshAgent { get; private set; }
    public AgentBody body => _agentBody;
    public AgentStatSetSO defaultStatSet => _defaultStatSet;

    // ITransientTargetAgent
    public ActorDescription transientTarget {
      get => _transientTarget;
      set {
        if (_transientTarget == value) return;
        _transientTarget = value;
        var nameOf = _transientTarget?.name ?? "NULL";
        Debug.Log($"Agent new target {nameOf}", transientTarget);
      }
    }
    public int transientTargetId => _transientTarget != null 
      ? _transientTarget.GetComponent<ActorId>()?.id ?? -1 
      : -1;

    // IInventoryAgent
    public ActorInventory inventory => _inventory;

    // IWorkAgent
    public AgentExperience experience => _experience;
    public AgentRecipes recipes => _recipes;
    public RecipeModule recipeModule => _recipeModule;
    public WorkPriority GetWorkScheduler() => _workPriority;
    
    public void AddExperience(int amount) {
      _experience.AddXP(amount);
    }

    // ICampAgent
    public CampLocation camp => agentBrain.memory.persistentMemory.Recall<CampLocation>(CampKeys.PERSONAL_CAMP);
    public AgentCampData campData => _campModule?.GetAgentCampData(this);

    // IGoapAgentCore
    public void StopAndCleanPath() {
      navMeshAgent.ResetPath();
      navMeshAgent.isStopped = true;
    }

    private void Awake() {
      RefreshLinks();
      _baseNavSpeed = navMeshAgent.speed;
    }

    private void OnEnable() {
      _simLoop?.Register(this);
      if (_simTime != null) _simTime.OnSpeedChanged += OnSimSpeedChanged;
    }

    private void OnDisable() {
      _simLoop?.Unregister(this);
      if (_simTime != null) _simTime.OnSpeedChanged -= OnSimSpeedChanged;
    }

    public void Tick() {
      UpdateAnimation();
    }

    public void SimTick(float simDeltaTime) {
      _agentBody.TickStats(simDeltaTime);
      _agentBrain.Tick(simDeltaTime);
    }

    private void OnSimSpeedChanged(SimSpeed speed) {
      var scale = _simTime.timeScale;
      navMeshAgent.speed = _baseNavSpeed * scale;

      var animController = _agentBody?.animationController;
      if (animController != null && animController.animator != null) {
        animController.animator.speed = scale;
      }
    }

    private void UpdateAnimation() {
      var animController = _agentBody?.animationController;
      if (animController == null) return;

      var maxSpeed = _baseNavSpeed * _sprintSpeedModifier;
      var speedNorm = navMeshAgent.velocity.magnitude / maxSpeed;
      animController.SetParams(speedNorm, GetRotation(), speedNorm < 0.05f);
    }

    private void OnValidate() {
      RefreshLinks();
    }

    private void RefreshLinks() {
      if (navMeshAgent == null) navMeshAgent = GetComponent<NavMeshAgent>();
      if (_agentBrain == null) _agentBrain = GetComponentInChildren<AgentBrain>();
      if (_agentBody == null) _agentBody = GetComponentInChildren<AgentBody>();
      if (_workPriority == null) _workPriority = GetComponentInChildren<WorkPriority>();
    }

    public void OnCreated() {
      agentBrain.Initialize(this);
      _agentBody.Initialize(this);
      
      _experience.OnLevelUp += _recipes.OnLevelUp;
      _recipes.Initialize(_experience.level);

      if (_simTime != null) {
        OnSimSpeedChanged(_simTime.currentSpeed);
      }
    }

    private float GetRotation() {
      var vel = navMeshAgent.velocity;
      if (vel.sqrMagnitude < 0.0001f) return 0.5f;

      var velDir = new Vector3(vel.x, 0f, vel.z).normalized;
      if (velDir == Vector3.zero) return 0.5f;

      var animController = _agentBody?.animationController;
      if (animController == null) return 0.5f;
      
      var angle = Vector3.SignedAngle(animController.transform.forward, velDir, Vector3.up);
      var normalized = angle / 360f + 0.5f;
      return Mathf.Clamp01(normalized);
    }
  }
}
