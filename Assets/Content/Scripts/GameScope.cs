using Content.Scripts.AI.Animals;
using Content.Scripts.AI.Craft;
using Content.Scripts.AI.GOAP;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.AI.GOAP.Planning;
using Content.Scripts.AI.Navigation;
using Content.Scripts.Building.Data;
using Content.Scripts.Building.Services;
using Content.Scripts.Building.Services.Visuals;
using Content.Scripts.Core.Environment;
using Content.Scripts.Core.Simulation;
using Content.Scripts.Game.Camera;
using Content.Scripts.Game.Camera.Settings;
using Content.Scripts.Game.Decay;
using Content.Scripts.Game.Effects;
using Content.Scripts.Game.Interaction;
using Content.Scripts.Game.Progression;
using Content.Scripts.Game.Harvesting;
using Content.Scripts.Game.Trees;
using Content.Scripts.Game.Work;
using Content.Scripts.Ui;
using Content.Scripts.Ui.Commands;
using Content.Scripts.Ui.Services;
using Content.Scripts.World;
using Content.Scripts.World.Grid.Presentation;
using Unity.Cinemachine;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts {
  public class GameScope : LifetimeScope {
    public CinemachineTargetGroup agentsRoot;
    public GOAPAgent agentPrefab;
    public CinemachineCamera cameraPrefab;
    public CameraSettingsSO cameraSettings;
    public EnvironmentSetupSO environmentSetup;
    public BuildingManagerConfigSO buildingManagerConfig;
    public TreeFallConfigSO treeFallConfig;
    public UILayer[] uiLayers;

    [Header("Building")] 
    public Material structureGhostMaterial;
    
    [Header("Progression")]
    public ColonyProgressionConfigSO colonyProgressionConfig;
    
    [Header("Debug")]
    public WorldGridPresentationConfigSO gridPresentationConfig;

    private Transform _uiRoot;


    protected override void Configure(IContainerBuilder builder) {
      base.Configure(builder);
      // Simulation
      builder.Register<SimulationTimeController>(Lifetime.Singleton).AsSelf();
      builder.RegisterEntryPoint<SimulationLoop>().AsSelf();

      builder.Register<ActorCreationModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      builder.Register<ActorDestructionModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      
      // Environment
      builder.RegisterInstance(environmentSetup).AsSelf();
      builder.Register<WorldEnvironment>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      builder.Register<WorldModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      builder.Register<WorldSaveModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      builder.Register<TreeModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      builder.RegisterInstance(treeFallConfig).AsSelf();
      builder.Register<HarvestModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      builder.Register<EffectsModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      builder.Register<AnimalsModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      builder.Register<NavigationModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();

      // Progression
      builder.RegisterInstance(colonyProgressionConfig).AsSelf();
      builder.Register<ColonyProgressionModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();

      // Decay & Work
      builder.RegisterEntryPoint<DecayModule>().AsSelf();
      builder.RegisterEntryPoint<WorkAssignmentModule>().AsSelf();
      builder.RegisterEntryPoint<WorkContextActionsRegistrar>();
      
      // Commands
      builder.RegisterEntryPoint<DebugCommandsRegistrar>();
      builder.RegisterEntryPoint<BuildCommandsRegistrar>();

      builder.Register<SelectionService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      builder.RegisterEntryPoint<SelectionInputHandler>();
      builder.Register<AgentContainerModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      builder.Register<LinearProgressionStrategy>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      builder.Register<FadeAnimationStrategy>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      builder.Register<StructureVisualsModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      

      // Building
      builder.RegisterInstance(structureGhostMaterial).Keyed("ghostMaterial").AsSelf();
      builder.RegisterInstance(buildingManagerConfig).AsSelf();
      builder.Register<StructurePlacementService>(Lifetime.Singleton).AsSelf();
      builder.Register<StructureConstructionService>(Lifetime.Singleton).AsSelf();
      builder.Register<ModulePlacementService>(Lifetime.Singleton).AsSelf();
      builder.Register<StructureExpansionService>(Lifetime.Singleton).AsSelf();
      builder.Register<StructuresModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();


      builder.RegisterComponent(agentPrefab).AsSelf();
      builder.Register<GoapFeatureBankModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      builder.Register<GoapPlanFactory>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      builder.Register<RecipeModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      builder.RegisterComponentInNewPrefab(agentsRoot, Lifetime.Scoped).AsSelf();
      builder.RegisterComponentInNewPrefab(cameraPrefab, Lifetime.Scoped).AsSelf();
      builder.RegisterInstance(cameraSettings).AsSelf();
      builder.Register<CameraModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      builder.Register<CutoutModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      builder.Register<DebugPanel.DebugModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
      
      // WorldGrid Presentation
      builder.RegisterInstance(gridPresentationConfig).AsSelf();
      builder.Register<WorldGridPresentationModule>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();

      InitUi(builder);
    }

    private void InitUi(IContainerBuilder builder) {
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
