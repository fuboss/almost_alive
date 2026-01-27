using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Game.Interaction;
using Content.Scripts.Ui.Layers.MainPanel;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.Ui {
  /// <summary>
  /// to del
  /// </summary>
  public class AgentUIModule : IStartable, ITickable, IInitializable {
    [Inject] private AgentContainerModule _agentContainerModule;
    [Inject] private UiModule _uiModule;

    public void Initialize() {
    }


    public void Start() {
    }

    public void Tick() {
    }
  }
}