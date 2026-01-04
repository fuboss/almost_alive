using System;
using System.Linq;
using Content.Scripts.AI.GOAP.Agent;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.Game.Interaction {
  public class ActorSelectionModule : IAgentSelectionModule, ITickable {
    [Inject] private AgentContainerModule _agentContainerModule;
    private IGoapAgent _selectedAgent;
    private RaycastHit[] _raycastResult = new RaycastHit[32];

    public event Action<IGoapAgent, IGoapAgent> OnSelectionChanged;

    public IGoapAgent GetSelectedAgent() {
      if (_agentContainerModule == null) {
        Debug.LogWarning("[ActorSelectionModule] Injection failed");
        return null;
      }

      return _agentContainerModule.agentDescriptions.Count == 0
        ? null
        : _agentContainerModule.agentDescriptions
          .FirstOrDefault(kvp => kvp.Value is ISelectableActor { isSelected: true }).Key;
    }

    public void SelectAgent(IGoapAgent agent) {
      if (agent == null) {
        ClearSelection();
        return;
      }

      _agentContainerModule.agentDescriptions.ToList().ForEach(kvp => {
        if (kvp.Value is ISelectableActor selectableActor) {
          selectableActor.isSelected = kvp.Key == agent;
        }
      });
    }

    public void SelectAgents(params IGoapAgent[] agents) {
      if (agents.Length == 0) {
        ClearSelection();
        return;
      }

      foreach (var agent in _agentContainerModule.agentDescriptions) {
        if (agent.Value is not ISelectableActor selectableActor) continue;
        selectableActor.isSelected = agents.Contains(agent.Key);
      }
    }

    public void ClearSelection() {
      foreach (var agent in _agentContainerModule.agentDescriptions) {
        if (agent.Value is not ISelectableActor selectableActor) continue;
        selectableActor.isSelected = false;
      }
    }

    public void Tick() {
      if (InputSystem.actions.FindAction("Click").WasReleasedThisFrame()) {
        TrySelectByMouse();
      }


      var selectedAgent = GetSelectedAgent();
      if (selectedAgent != _selectedAgent) {
        OnSelectionChanged?.Invoke(selectedAgent, _selectedAgent);
        _selectedAgent = selectedAgent;
        Debug.Log(
          $"[ActorSelectionModule]Selection changed to {(_selectedAgent != null ? _selectedAgent.gameObject.name : "null")}");
      }
    }

    private void TrySelectByMouse() {
      var castedAgent = RaycastAgentUnderMouse();
      if (castedAgent != null) {
        SelectAgent(castedAgent);
        return;
      }

      ClearSelection();
    }

    private IGoapAgent RaycastAgentUnderMouse() {
      var ray = Camera.main!.ScreenPointToRay(Mouse.current.position.ReadValue());
      var count = Physics.RaycastNonAlloc(ray, _raycastResult);
      if (count <= 0) {
        return null;
      }

      for (var i = 0; i < count; i++) {
        var hitInfo = _raycastResult[i];
        var agent = GetSelectableAgentFromHit(hitInfo);
        if (agent != null) {
          return agent;
        }
      }

      return null;
    }

    private IGoapAgent GetSelectableAgentFromHit(RaycastHit hitInfo) {
      if (hitInfo.collider.isTrigger) return null;
      var selectableActor = hitInfo.collider.GetComponentInParent<ISelectableActor>();
      return selectableActor is { canSelect: true } ? selectableActor.gameObject.GetComponent<IGoapAgent>() : null;
    }
  }
}