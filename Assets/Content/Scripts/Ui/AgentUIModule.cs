using Content.Scripts.AI.GOAP.Agent;

using Content.Scripts.Game.Interaction;
using Content.Scripts.Ui.Layers.MainPanel;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.Ui {
  public class AgentUIModule : IStartable, ITickable, IInitializable {
    [Inject] private AgentContainerModule _agentContainerModule;
    [Inject] private ActorSelectionModule _agentSelectionModule;
    [Inject] private UiModule _uiModule;
    [Inject] private MainInfoPanel _mainInfoPanelUI;
    
    public void Initialize() {
      if (_agentSelectionModule != null) _agentSelectionModule.OnSelectionChanged += OnAgentSelection;
      else {
        Debug.LogError("[AgentUIModule] Injection failed for ActorSelectionModule");
      }
    }

    private void OnAgentSelection(IGoapAgent current, IGoapAgent previous) {
      if (current == null) {
        _uiModule.RemoveLayer(_mainInfoPanelUI);
        return;
      }
      _uiModule.AddLayer(_mainInfoPanelUI);
      _mainInfoPanelUI.SetAgent(current);
    }

    public void Start() {
    }

    public void Tick() {
    }
  }
}