using Content.Scripts.AI.Camp;
using Content.Scripts.AI.GOAP;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Planning;
using Content.Scripts.Core.Environment;
using Content.Scripts.Core.Simulation;
using Content.Scripts.Game;
using Content.Scripts.Game.Decay;
using Content.Scripts.Game.Interaction;
using Content.Scripts.Game.Work;
using Content.Scripts.Ui;
using Unity.Cinemachine;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts {
  public class GameScope : LifetimeScope {
    public CinemachineTargetGroup agentsRoot;
    public GOAPAgent agentPrefab;
    public CinemachineCamera cameraPrefab;
    public EnvironmentSetupSO environmentSetup;
    public UILayer[] uiLayers;
    private Transform _uiRoot;
    

    protected override void Configure(IContainerBuilder builder) {
      base.Configure(builder);
      // Simulation
      builder.Register<SimulationTimeController>(Lifetime.Singleton).AsSelf();
      builder.RegisterEntryPoint<SimulationLoop>().AsSelf();
      
      // Environment
      builder.RegisterInstance(environmentSetup).AsSelf();
      builder.Register<WorldEnvironment>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      
      
      // Decay & Work
      builder.RegisterEntryPoint<DecayModule>().AsSelf();
      builder.RegisterEntryPoint<WorkAssignmentModule>().AsSelf();

      builder.Register<GoapPlanFactory>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      builder.Register<AgentFactory>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      
      builder.Register<ActorSelectionModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      builder.Register<AgentContainerModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      builder.Register<ActorCreationModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      builder.Register<ActorDestructionModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      builder.Register<CampModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      
      builder.RegisterComponent(agentPrefab).AsSelf();
      builder.Register<GoapFeatureBankModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      builder.RegisterComponentInNewPrefab(agentsRoot, Lifetime.Scoped).AsSelf();
      builder.RegisterComponentInNewPrefab(cameraPrefab, Lifetime.Scoped).AsSelf();
      
      
      builder.Register<AgentUIModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      builder.Register<GameUIModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      
      //ui
      builder.Register<UiModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      foreach (var uiLayer in uiLayers) {
        builder.RegisterComponentInNewPrefab(uiLayer, Lifetime.Singleton)
          .UnderTransform(() => {
            if (_uiRoot == null) _uiRoot = new GameObject("UI Root").transform;
            return _uiRoot;
          })
          .DontDestroyOnLoad()
          .AsImplementedInterfaces()
          .AsSelf();
      }
    }
  }
}