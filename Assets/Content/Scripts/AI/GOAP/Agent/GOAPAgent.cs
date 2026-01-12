using Content.Scripts.Animation;
using ImprovedTimers;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AI;

namespace Content.Scripts.AI.GOAP.Agent {
  [RequireComponent(typeof(NavMeshAgent))]
  public class GOAPAgent : SerializedMonoBehaviour, IGoapAgent {
    [SerializeField] private AgentStatSetSO _defaultStatSet;
    [SerializeField] private AgentBrain _agentBrain;
    [SerializeField] private ActorInventory _inventory;

    [SerializeField] private float _statsUpdateInterval = 1f;
    [SerializeField] private float _sprintSpeedModifier = 1.5f;
    private Vector3 _destination;
    private CountdownTimer _statsTimer;
    private GameObject _target;
    private AgentBody _agentBody;
    [ShowInInspector, ReadOnly] private GameObject _transientTarget;

    private void Awake() {
      RefreshLinks();
    }

    private void Start() {
    }

    private void Update() {
      //_statsTimer.Tick();
      _agentBody.TickStats(Time.deltaTime);
      _agentBrain.Tick(Time.deltaTime);

      var speedNorm = navMeshAgent.velocity.magnitude / (navMeshAgent.speed * _sprintSpeedModifier);
      animationController.SetParams(speedNorm, GetRotation(), speedNorm < 0.05);
    }

    private void OnValidate() {
      RefreshLinks();
    }

    public AgentBrain agentBrain => _agentBrain;
    public NavMeshAgent navMeshAgent { get; private set; }

    public new Rigidbody rigidbody { get; private set; }

    public AnimationController animationController { get; private set; }

    public ActorInventory inventory => _inventory;

    public GameObject transientTarget {
      get => _transientTarget;
      set {
        if (_transientTarget == value) return;
        //Debug.Log($"TransientTarget set {(value != null ? value.name : "null")} on agent {name}", gameObject);
        _transientTarget = value;
      }
    }

    public AgentBody body => _agentBody;

    public AgentStatSetSO defaultStatSet => _defaultStatSet;

    private void RefreshLinks() {
      if (navMeshAgent == null) navMeshAgent = GetComponent<NavMeshAgent>();
      if (animationController == null) animationController = GetComponentInChildren<AnimationController>();
      if (rigidbody == null) rigidbody = GetComponent<Rigidbody>();
      if (_agentBrain == null) _agentBrain = GetComponentInChildren<AgentBrain>();
      if (_agentBody == null) _agentBody = GetComponentInChildren<AgentBody>();
    }

    public void OnCreated() {
      agentBrain.Initialize(this);
      _agentBody.Initialize(this);
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

    // private void SetupTimers() {
    //   _statsTimer = new CountdownTimer(_statsUpdateInterval);
    //   _statsTimer.OnTimerStop += () => {
    //     UpdateStats();
    //     _statsTimer.Start();
    //   };
    //   _statsTimer.Start();
    // }
  }
}