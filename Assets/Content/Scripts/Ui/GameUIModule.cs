using System;
using Content.Scripts.Game;
using Content.Scripts.Game.Craft;
using Content.Scripts.Game.Trees;
using Content.Scripts.Ui.Layers.ControlsPanel;
using Content.Scripts.Ui.Layers.WorldSpaceUI;
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

      ActorRegistry<UnfinishedActorBase>.onRegistered += OnUnfinishedCreated;
      ActorRegistry<UnfinishedActorBase>.onUnregistered += OnUnfinishedRemoved;

      ActorRegistry<ChoppingProgress>.onRegistered += OnChoppingStarted;
      ActorRegistry<ChoppingProgress>.onUnregistered += OnChoppingEnded;
    }

    private void OnUnfinishedCreated(UnfinishedActorBase actor) => 
      _worldSpaceUI.CreateProgressBar(actor);

    private void OnChoppingStarted(ChoppingProgress choppingProgress) =>
      _worldSpaceUI.CreateProgressBar(choppingProgress);

    private void OnChoppingEnded(ChoppingProgress choppingProgress) =>
      _worldSpaceUI.UnregisterWidgetsWithTarget(choppingProgress.actor.transform);

    private void OnUnfinishedRemoved(UnfinishedActorBase actor) =>
      _worldSpaceUI.UnregisterWidgetsWithTarget(actor.actor.transform);

    public void Tick() {
    }

    public void Dispose() {
      ActorRegistry<UnfinishedActorBase>.onRegistered -= OnUnfinishedCreated;
      ActorRegistry<UnfinishedActorBase>.onUnregistered -= OnUnfinishedRemoved;

      ActorRegistry<ChoppingProgress>.onRegistered -= OnChoppingStarted;
      ActorRegistry<ChoppingProgress>.onUnregistered -= OnChoppingEnded;
    }
  }
}