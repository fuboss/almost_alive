using System;
using Content.Scripts.AI.Craft;
using Content.Scripts.Animation;
using Content.Scripts.Core.Simulation;
using Content.Scripts.Game;
using Content.Scripts.Game.Work;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.AI.GOAP.Agent {
  [RequireComponent(typeof(NavMeshAgent))]
  public class GOAPAgent : SerializedMonoBehaviour, IGoapAgent, ISimulatable {
    [Inject] private SimulationLoop _simLoop;
    [Inject] private SimulationTimeController _simTime;
    [Inject] private RecipeModule _recipeModule;

    [SerializeField] private AgentStatSetSO _defaultStatSet;
    [SerializeField] private AgentBrain _agentBrain;
    [SerializeField] private ActorInventory _inventory;
    [SerializeField] private WorkPriority _workPriority;
    [SerializeField] private float _sprintSpeedModifier = 1.5f;
    
    [FoldoutGroup("Progression")]
    [SerializeField] private AgentExperience _experience = new();
    [FoldoutGroup("Progression")]
    [SerializeField] private AgentRecipes _recipes = new();
    
    [ShowInInspector, ReadOnly] private ActorDescription _transientTarget;
    [ShowInInspector, ReadOnly] private float _baseNavSpeed;

    private AgentBody _agentBody;

    public int tickPriority => 0;

    public AgentBrain agentBrain => _agentBrain;
    public NavMeshAgent navMeshAgent { get; private set; }
    public new Rigidbody rigidbody { get; private set; }
    public AnimationController animationController { get; private set; }
    public ActorInventory inventory => _inventory;
    public AgentExperience experience => _experience;
    public AgentRecipes recipes => _recipes;
    public ActorDescription transientTarget {
      get => _transientTarget;
      set {
        if (_transientTarget == value) return;
        _transientTarget = value;
      }
    }

    public int transientTargetId => _transientTarget.GetComponent<ActorId>()?.id ?? -1;
    public WorkPriority GetWorkScheduler() => _workPriority;

    public AgentBody body => _agentBody;
    public AgentStatSetSO defaultStatSet => _defaultStatSet;

    public RecipeModule recipeModule => _recipeModule;

    public void AddExperience(int amount) {
      _experience.AddXP(amount);
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
      // Visual-only updates (render time)
      UpdateAnimation();
    }

    public void SimTick(float simDeltaTime) {
      _agentBody.TickStats(simDeltaTime);
      _agentBrain.Tick(simDeltaTime);
    }

    private void OnSimSpeedChanged(SimSpeed speed) {
      var scale = _simTime.timeScale;
      navMeshAgent.speed = _baseNavSpeed * scale;

      if (animationController != null && animationController.animator != null) {
        animationController.animator.speed = scale;
      }
    }

    private void UpdateAnimation() {
      if (animationController == null) return;

      var maxSpeed = _baseNavSpeed * _sprintSpeedModifier;
      var speedNorm = navMeshAgent.velocity.magnitude / maxSpeed;
      animationController.SetParams(speedNorm, GetRotation(), speedNorm < 0.05f);
    }

    private void OnValidate() {
      RefreshLinks();
    }

    private void RefreshLinks() {
      if (navMeshAgent == null) navMeshAgent = GetComponent<NavMeshAgent>();
      if (animationController == null) animationController = GetComponentInChildren<AnimationController>();
      if (rigidbody == null) rigidbody = GetComponent<Rigidbody>();
      if (_agentBrain == null) _agentBrain = GetComponentInChildren<AgentBrain>();
      if (_agentBody == null) _agentBody = GetComponentInChildren<AgentBody>();
      if (_workPriority == null) _workPriority = GetComponentInChildren<WorkPriority>();
    }

    public void OnCreated() {
      agentBrain.Initialize(this);
      _agentBody.Initialize(this);
      // Initialize progression
      _experience.OnLevelUp += _recipes.OnLevelUp;
      _recipes.Initialize(_experience.level);

      // Apply initial time scale
      if (_simTime != null) {
        OnSimSpeedChanged(_simTime.currentSpeed);
      }
    }

    private float GetRotation() {
      var vel = navMeshAgent.velocity;
      if (vel.sqrMagnitude < 0.0001f) return 0.5f;

      var velDir = new Vector3(vel.x, 0f, vel.z).normalized;
      if (velDir == Vector3.zero) return 0.5f;

      var angle = Vector3.SignedAngle(animationController.transform.forward, velDir, Vector3.up);
      var normalized = angle / 360f + 0.5f;
      return Mathf.Clamp01(normalized);
    }
  }
}