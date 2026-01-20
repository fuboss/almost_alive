using System;
using Content.Scripts.Game;
using Content.Scripts.Game.Craft;
using Content.Scripts.Ui.Layers.ControlsPanel;
using Content.Scripts.Ui.Layers.WorldSpaceUI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.Ui {
  public class GameUIModule : IStartable, ITickable, IInitializable, IDisposable {
    [Inject] private UiModule _uiModule;
    [Inject] private ControlsPanelLayer _controlsPanelLayer;
    [Inject] private WorldSpaceUI _worldSpaceUI;
    
    public void Initialize() {
    }

    public void Start() {
      _uiModule.AddLayer(_controlsPanelLayer);
      _uiModule.AddLayer(_worldSpaceUI);

      ActorRegistry<UnfinishedActor>.onRegistered += OnUnfinishedCreated;
      ActorRegistry<UnfinishedActor>.onUnregistered += OnUnfinishedRemoved;
    }

    private void OnUnfinishedCreated(UnfinishedActor actor) {
      Debug.LogError($"UI should show progress widget for unfinished actor {actor.name}");
      _worldSpaceUI.CreateProgressBar(actor);
    }
    
    private void OnUnfinishedRemoved(UnfinishedActor actor) {
      _worldSpaceUI.UnregisterWidgetsWithActor(actor);
      Debug.LogError($"UI should remove progress widget for unfinished actor {actor.name}");
    }

    public void Tick() {
    }

    public void Dispose() {
      ActorRegistry<UnfinishedActor>.onRegistered -= OnUnfinishedCreated;
      ActorRegistry<UnfinishedActor>.onUnregistered -= OnUnfinishedRemoved;
    }
  }
}