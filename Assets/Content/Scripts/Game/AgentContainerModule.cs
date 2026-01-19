using System.Collections.Generic;
using Content.Scripts.Game;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.AI.GOAP.Agent {
  public class AgentContainerModule : ITickable, IStartable {
    [Inject] private IAgentFactory _agentFactory;
    [Inject] private CinemachineTargetGroup _agentsRoot;
    [Inject] private CinemachineCamera _camera;

    private readonly List<IGoapAgent> _agents = new();
    private readonly Dictionary<IGoapAgent, IActorDescription> _agentDescriptions = new();

    public IReadOnlyDictionary<IGoapAgent, IActorDescription> agentDescriptions => _agentDescriptions;

    public void Add(IGoapAgent instance) {
      if (_agents.Contains(instance)) {
        return;
      }

      _agents.Add(instance);
      _agentDescriptions[instance] = instance.gameObject.GetComponent<IActorDescription>();
      Debug.Log($"[AgentContainerModule] Agent added to container. Total agents: {_agents.Count}",
        instance.gameObject);
    }

    public void Remove(IGoapAgent instance) {
      if (!_agents.Contains(instance)) {
        return;
      }

      _agents.Remove(instance);
      _agentDescriptions.Remove(instance);
      Debug.Log($"[AgentContainerModule] Agent removed from container. Total agents: {_agents.Count}",
        instance.gameObject);
    }

    public void Tick() {
      if (Keyboard.current.enterKey.wasReleasedThisFrame) {
        SpawnNewAgent(Vector3.zero + Random.onUnitSphere * 5);
      }

      //todo move this into a separate system
      foreach (var goapAgent in _agents) {
        if(goapAgent == null) continue;
        goapAgent.Tick();
      }
    }

    private void SpawnNewAgent(Vector3 position) {
      if (!Physics.Raycast(position + Vector3.up * 100, Vector3.down, out RaycastHit hit)) return;
      if (_agentsRoot != null) {
        
        var instance = _agentFactory.Spawn(hit.point);
        Add(instance);
        AddToCameraGroup(instance);
      }
      else {
        Debug.LogError("np factory resolved");
      }
    }

    private void AddToCameraGroup(GOAPAgent instance) {
      var camGroup = _agentsRoot.GetComponent<CinemachineTargetGroup>();
      camGroup.Targets ??= new List<CinemachineTargetGroup.Target>();
      camGroup.Targets.Add(new CinemachineTargetGroup.Target() {
        Object = instance.transform,
        Radius = 5,
        Weight = 1
      });
    }

    public void Start() {
      _camera.Target = new CameraTarget() {
        CustomLookAtTarget = false,
        LookAtTarget = _agentsRoot.transform,
        TrackingTarget = _agentsRoot.transform
      };
      _camera.UpdateTargetCache();
    }
  }
}