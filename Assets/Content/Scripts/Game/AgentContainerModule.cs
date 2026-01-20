using System.Collections.Generic;
using Content.Scripts.AI.Camp;
using Content.Scripts.AI.GOAP.Agent.Camera;
using Content.Scripts.Game;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityUtils;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.AI.GOAP.Agent {
  public class AgentContainerModule : ITickable, IStartable {
    [Inject] private IAgentFactory _agentFactory;
    [Inject] private CameraModule _cameraModule;

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
      _cameraModule.AddToCameraGroup(instance);
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
      //input to spawn new agent(debug) -> move to separate input system later
      if (Keyboard.current.enterKey.wasReleasedThisFrame) {
        var randCamp = Registry<CampLocation>.GetAll().Random();
        var spawnPos = randCamp != null
          ? randCamp.transform.position + Random.onUnitSphere * 5 + Vector3.up * 3
          : Vector3.zero + Random.onUnitSphere * 5;
        SpawnNewAgent(spawnPos);
      }

      //todo move this into a separate system
      foreach (var goapAgent in _agents) {
        if (goapAgent == null) continue;
        goapAgent.Tick();
      }
    }

    private void SpawnNewAgent(Vector3 position) {
      if (!Physics.Raycast(position + Vector3.up * 100, Vector3.down, out RaycastHit hit)) return;

      var instance = _agentFactory.Spawn(hit.point);
      Add(instance);
    }
  

    public void Start() {
     
    }
  }
}