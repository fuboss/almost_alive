using Content.Scripts.AI.Animals;
using Content.Scripts.Core.Simulation;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;
using VContainer;

namespace Content.Scripts.AI.GOAP.Agent {
  /// <summary>
  /// Simplified agent for animals (deer, wolves, rabbits, etc.)
  /// Uses GOAP for behavior but no inventory, work, or camp systems.
  /// </summary>
  [RequireComponent(typeof(NavMeshAgent))]
  public class AnimalAgent : SerializedMonoBehaviour, IGoapAnimalAgent, ISimulatable {
    [Inject] private SimulationLoop _simLoop;
    [Inject] private SimulationTimeController _simTime;

    [SerializeField] private AgentStatSetSO _defaultStatSet;
    [SerializeField] private AnimalBrain _animalBrain;
    [SerializeField] private AgentBody _agentBody;
    [SerializeField] private HerdingBehavior _herdingBehavior;
    [SerializeField] private float _baseSpeed = 3f;

    [FoldoutGroup("Herd")] [SerializeField]
    private int _herdId = -1;

    public int tickPriority => 10;

    // IGoapAgentCore - concrete access
    public AnimalBrain brain => _animalBrain;

    // IGoapAgentCore - interface implementation
    IAgentBrain IGoapAgentCore.agentBrain => _animalBrain;

    public NavMeshAgent navMeshAgent { get; private set; }
    public AgentBody body => _agentBody;
    public AgentStatSetSO defaultStatSet => _defaultStatSet;

    // IHerdMember
    public int herdId {
      get => _herdId;
      set => _herdId = value;
    }

    public HerdingBehavior herdingBehavior => _herdingBehavior;

    public void StopAndCleanPath() {
      navMeshAgent.ResetPath();
      navMeshAgent.isStopped = true;
    }

    private void Awake() {
      RefreshLinks();
      if (navMeshAgent != null) {
        _baseSpeed = navMeshAgent.speed;
      }
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
      _agentBody?.TickStats(simDeltaTime);
      _animalBrain?.Tick(simDeltaTime);
    }

    private void OnSimSpeedChanged(SimSpeed speed) {
      var scale = _simTime.timeScale;
      if (navMeshAgent != null) {
        navMeshAgent.speed = _baseSpeed * scale;
      }

      var animController = _agentBody?.animationController;
      if (animController != null && animController.animator != null) {
        animController.animator.speed = scale;
      }
    }

    private void UpdateAnimation() {
      var animController = _agentBody?.animationController;
      if (animController == null || navMeshAgent == null) return;

      var speedNorm = navMeshAgent.velocity.magnitude / _baseSpeed;
      animController.SetParams(speedNorm, 0.5f, speedNorm < 0.05f);
    }

    private void OnValidate() {
      RefreshLinks();
    }

    private void RefreshLinks() {
      if (navMeshAgent == null) navMeshAgent = GetComponent<NavMeshAgent>();
      if (_animalBrain == null) _animalBrain = GetComponentInChildren<AnimalBrain>();
      if (_agentBody == null) _agentBody = GetComponentInChildren<AgentBody>();
      if (_herdingBehavior == null) _herdingBehavior = GetComponent<HerdingBehavior>();
    }

    public void OnCreated() {
      _animalBrain?.Initialize(this);
      _agentBody?.Initialize(this);

      if (_simTime != null) {
        OnSimSpeedChanged(_simTime.currentSpeed);
      }
    }
  }
}
