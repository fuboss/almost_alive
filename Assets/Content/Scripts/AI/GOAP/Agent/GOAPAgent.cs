using Content.Scripts.AI.GOAP.Core;
using Content.Scripts.Animation;
using ImprovedTimers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace Content.Scripts.AI.GOAP.Agent {
  public interface IGoapAgent {
    AgentBrain agentBrain { get; }
    NavMeshAgent navMeshAgent { get; }
    Rigidbody rigidbody { get; }
    AnimationController animationController { get; }

    public Vector3 position => navMeshAgent.transform.position;
    public Vector3 nextPosition => navMeshAgent.nextPosition;
    
    public AgentBelief GetBelief(string beliefName) {
      return agentBrain.beliefs.Get(beliefName);
    }
  }

  [RequireComponent(typeof(NavMeshAgent))]
  public class GOAPAgent : SerializedMonoBehaviour, IGoapAgent {
    public AgentBrain _agentBrain;

    [Header("Known Locations")] [SerializeField]
    private Transform _restingPosition;

    [SerializeField] private Transform _foodShack;
    [SerializeField] private Transform _doorOnePosition;
    [SerializeField] private Transform _doorTwoPosition;

    [Header("Stats")] public float health = 100;
    public float stamina = 100;
    private AnimationController _animations;
    private Vector3 _destination;

    private NavMeshAgent _navMeshAgent;
    private Rigidbody _rb;

    private CountdownTimer _statsTimer;

    private GameObject _target;

    public AgentBrain agentBrain => _agentBrain;
    public NavMeshAgent navMeshAgent => _navMeshAgent;
    public new Rigidbody rigidbody => _rb;
    public AnimationController animationController => _animations;

    private void Awake() {
      RefreshLinks();
    }

    private void OnValidate() {
      RefreshLinks();
    }

    private void RefreshLinks() {
      if (_navMeshAgent == null) _navMeshAgent = GetComponent<NavMeshAgent>();
      if (_animations == null) _animations = GetComponent<AnimationController>();
      if (_rb == null) _rb = GetComponent<Rigidbody>();
      if (_agentBrain == null) _agentBrain = GetComponent<AgentBrain>();
    }

    public void OnCreated() {
      agentBrain.Initialize(this);
    }

    private void Start() {
      SetupTimers();
    }

    private void Update() {
      _statsTimer.Tick();
      _animations.SetSpeed(_navMeshAgent.velocity.magnitude);
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
      // stamina += InRangeOf(_restingPosition.position, 3f) ? 20 : -10;
      // health += InRangeOf(_foodShack.position, 3f) ? 20 : -5;
      // stamina = Mathf.Clamp(stamina, 0, 100);
      // health = Mathf.Clamp(health, 0, 100);
    }
  }
}