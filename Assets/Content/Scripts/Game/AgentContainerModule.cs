using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Agent.Camera;
using Content.Scripts.Game;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.AI.GOAP.Agent {
  //todo: this module seems strange, maybe refactor later to CameraModule
  public class AgentContainerModule : ITickable, IStartable {
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
      //todo move this into a separate system
      foreach (var goapAgent in _agents) {
        goapAgent?.Tick();
      }
    }

    public void Start() {
     
    }
  }
}