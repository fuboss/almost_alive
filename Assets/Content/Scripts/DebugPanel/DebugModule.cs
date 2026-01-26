using System.Linq;
using Content.Scripts.AI;
using Content.Scripts.AI.GOAP;
using Content.Scripts.Building.Runtime;
using Content.Scripts.Building.Services;
using Content.Scripts.Core.Simulation;
using Content.Scripts.Game;
using Content.Scripts.Game.Trees;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace Content.Scripts.DebugPanel {
  public class DebugModule : IStartable, ILateTickable {
    [Inject] private ActorCreationModule _actorCreation;
    [Inject] private ActorDestructionModule _actorDestruction;
    [Inject] private SimulationTimeController _simTime;
    [Inject] private StructuresModule _structuresModule;
    [Inject] private StructurePlacementService _placement;
    [Inject] private ModulePlacementService _modulePlacement;
    [Inject] private TreeModule _treeModule;

    private DebugActionRegistry _registry;
    private DebugState _currentState = DebugState.Idle;
    private IDebugAction _pendingAction;
    private DebugActionContext _pendingContext;

    private readonly RaycastHit[] _raycastBuffer = new RaycastHit[32];
    private LayerMask GROUND_LAYER_MASK = LayerMask.GetMask("Terrain", "Water");

    public event System.Action<DebugState> OnStateChanged;
    public event System.Action<IDebugAction> OnActionSelected;
    public event System.Action OnActionCancelled;

    public DebugState CurrentState => _currentState;
    public IDebugAction PendingAction => _pendingAction;
    public DebugActionRegistry Registry => _registry;
    public ActorCreationModule ActorCreation => _actorCreation;
    public ActorDestructionModule ActorDestruction => _actorDestruction;
    public SimulationTimeController SimTime => _simTime;

    void IStartable.Start() {
      _registry = new DebugActionRegistry();
      RegisterDefaultActions();

      // Try to register spawn actions immediately
      TryRegisterSpawnActions();
      TryRegisterStructureActions();
      
      // Subscribe to module loading
      _structuresModule.OnModulesLoaded += RegisterModuleActions;
    }

    void ILateTickable.LateTick() {
      HandleInput();

      // Periodically check if actors have been loaded
      if (!_registry.GetActionsByCategory(DebugCategory.Spawn).Any() && _actorCreation.IsInitialized) {
        TryRegisterSpawnActions();
      }

      // Periodically check if actors have been loaded
      if (!_registry.GetActionsByCategory(DebugCategory.Structure).Any() && _structuresModule.isInitialized) {
        TryRegisterStructureActions();
      }
    }

    public void TogglePanel() {
      if (CurrentState == DebugState.Idle) {
        //_uiModule.AddLayer(DebugPanelLayer.Instance);
        SetState(DebugState.Browsing);
      }
      else {
        //_uiModule.RemoveLayer(DebugPanelLayer.Instance);
        SetState(DebugState.Idle);
        // Cancel any pending action
        if (CurrentState == DebugState.ReadyToApply) {
          CancelAction();
        }
      }
    }

    private void HandleInput() {
      if (Keyboard.current.f12Key.wasPressedThisFrame) {
        TogglePanel();
        return;
      }

      // ESC - cancel current action
      if (_currentState == DebugState.ReadyToApply && Keyboard.current.escapeKey.wasPressedThisFrame) {
        CancelAction();
        return;
      }

      // Handle click to apply action
      if (_currentState == DebugState.ReadyToApply && Mouse.current.leftButton.wasPressedThisFrame) {
        // Check if click is not on UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) {
          return;
        }

        TryApplyPendingAction();
      }
    }

    public void SetState(DebugState newState) {
      if (_currentState == newState) return;

      _currentState = newState;
      OnStateChanged?.Invoke(_currentState);

      Debug.Log($"[DebugModule] State changed to: {_currentState}");
    }

    public void SelectAction(IDebugAction action) {
      if (action == null) return;

      _pendingAction = action;
      _pendingContext = new DebugActionContext();

      // If action is instant - execute immediately
      if (action.actionType == DebugActionType.Instant) {
        action.Execute(_pendingContext);
        _pendingAction = null;
        OnActionSelected?.Invoke(action);
        return;
      }

      // Otherwise transition to waiting mode
      SetState(DebugState.ReadyToApply);
      OnActionSelected?.Invoke(action);
    }

    public void CancelAction() {
      _pendingAction = null;
      _pendingContext = null;
      SetState(DebugState.Browsing);
      OnActionCancelled?.Invoke();

      Debug.Log("[DebugModule] Action cancelled");
    }

    private void TryApplyPendingAction() {
      if (_pendingAction == null) return;

      var actionType = _pendingAction.actionType;

      switch (actionType) {
        case DebugActionType.RequiresWorldPosition:
          if (TryGetWorldPosition(out Vector3 worldPos)) {
            _pendingContext.worldPosition = worldPos;
            _pendingAction.Execute(_pendingContext);
            Debug.Log($"[DebugModule] Executed {_pendingAction.displayName} at {worldPos}");
            ResetAfterAction();
          }

          break;

        case DebugActionType.RequiresActor:
          if (TryGetActorUnderMouse(out ActorDescription actor)) {
            _pendingContext.targetActor = actor;
            _pendingAction.Execute(_pendingContext);
            Debug.Log($"[DebugModule] Executed {_pendingAction.displayName} on {actor.name}");
            ResetAfterAction();
          }
          else {
            CancelAction();
          }
          break;

        case DebugActionType.RequiresStructure:
          if (TryGetStructureUnderMouse(out Structure structure)) {
            _pendingContext.targetStructure = structure;
            _pendingAction.Execute(_pendingContext);
            Debug.Log($"[DebugModule] Executed {_pendingAction.displayName} on {structure.name}");
            ResetAfterAction();
          }
          else {
            CancelAction();
          }
          break;
      }
    }

    private void ResetAfterAction() {
      _pendingAction = null;
      _pendingContext = null;
      SetState(DebugState.Browsing);
    }

    private bool TryGetWorldPosition(out Vector3 worldPosition) {
      worldPosition = Vector3.zero;

      if (Camera.main == null) return false;

      Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
      int hitCount = Physics.RaycastNonAlloc(ray, _raycastBuffer, 1000f, GROUND_LAYER_MASK);

      if (hitCount > 0) {
        worldPosition = _raycastBuffer[0].point;
        return true;
      }

      return false;
    }

    private bool TryGetActorUnderMouse(out ActorDescription actor) {
      actor = null;

      if (Camera.main == null) return false;

      Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
      int hitCount = Physics.RaycastNonAlloc(ray, _raycastBuffer, 1000f);

      for (int i = 0; i < hitCount; i++) {
        actor = _raycastBuffer[i].collider.GetComponentInParent<ActorDescription>();
        if (actor != null) {
          return true;
        }
      }

      return false;
    }

    private bool TryGetStructureUnderMouse(out Structure structure) {
      structure = null;

      if (Camera.main == null) return false;

      Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
      int hitCount = Physics.RaycastNonAlloc(ray, _raycastBuffer, 1000f);

      for (int i = 0; i < hitCount; i++) {
        structure = _raycastBuffer[i].collider.GetComponentInParent<Structure>();
        if (structure != null) {
          return true;
        }
      }

      return false;
    }

    private void RegisterDefaultActions() {
      // Destroy actions
      _registry.Register(new Actions.DestroyActorAction(_actorDestruction));

      // Environment actions
      _registry.Register(new Actions.SetTimeAction(_simTime, 6f, "Set Time to Dawn (06:00)"));
      _registry.Register(new Actions.SetTimeAction(_simTime, 12f, "Set Time to Noon (12:00)"));
      _registry.Register(new Actions.SetTimeAction(_simTime, 18f, "Set Time to Dusk (18:00)"));
      _registry.Register(new Actions.SetTimeAction(_simTime, 0f, "Set Time to Midnight (00:00)"));
      _registry.Register(new Actions.StartFireAction());
      _registry.Register(new Actions.ExtinguishFiresAction());
      _registry.Register(new Actions.ChopTreeAction(_treeModule));
    }

    private void TryRegisterSpawnActions() {
      if (!_actorCreation.IsInitialized) {
        return;
      }

      var allPrefabs = _actorCreation.allPrefabs;
      if (allPrefabs == null) return;

      int count = 0;
      foreach (var prefab in allPrefabs) {
        var actorDesc = prefab;
        if (actorDesc == null) continue;
        var displayName = $"Spawn {actorDesc.actorKey}";
        _registry.Register(new Actions.SpawnActorAction(_actorCreation, _structuresModule, _placement,
          actorDesc.actorKey, displayName));
        count++;
      }

      if (count > 0) {
        Debug.Log($"[DebugModule] Registered {count} spawn actions");
      }
    }

    private void TryRegisterStructureActions() {
      if (!_structuresModule.isInitialized) {
        return;
      }

      var allSo = _structuresModule.definitions;
      if (allSo == null) return;

      int count = 0;
      foreach (var definitionSO in allSo) {
        if (definitionSO == null) continue;
        var displayName = $"Place {definitionSO.structureId}";
        _registry.Register(new Actions.SpawnStructureAction(_structuresModule, definitionSO, displayName));
        count++;
      }

      if (count > 0) {
        Debug.Log($"[DebugModule] Registered {count} structure actions");
      }

      TryRegisterSpawnTemplates();
    }

    private void TryRegisterSpawnTemplates() {
      if (!_structuresModule.isInitialized || !_actorCreation.IsInitialized) {
        return;
      }

      var firstStructure = _structuresModule.definitions?.FirstOrDefault();
      var firstActor = _actorCreation.allPrefabs?.FirstOrDefault(p => p.GetDefinition(Tag.AGENT));

      if (firstStructure == null || firstActor == null) return;

      var displayName = $"Structure + Agent ({firstStructure.structureId} + {firstActor.actorKey})";
      _registry.Register(new Actions.SpawnStructureWithAgentAction(
        _structuresModule,
        _actorCreation,
        firstStructure,
        firstActor.actorKey,
        displayName));

      Debug.Log($"[DebugModule] Registered SpawnTemplates action: {displayName}");
    }

    public void RegisterModuleActions(Building.Data.ModuleDefinitionSO[] moduleDefinitions) {
      if (moduleDefinitions == null || moduleDefinitions.Length == 0) return;

      int count = 0;
      foreach (var moduleDef in moduleDefinitions) {
        if (moduleDef == null) continue;
        
        var footprintInfo = $"({moduleDef.slotFootprint.x}x{moduleDef.slotFootprint.y})";
        var clearanceInfo = moduleDef.clearanceRadius > 0 ? $" c{moduleDef.clearanceRadius}" : "";
        
        // Instant placement (debug/cheat)
        var instantName = $"[Instant] {moduleDef.moduleId} {footprintInfo}{clearanceInfo}";
        _registry.Register(new Actions.PlaceModuleAction(_modulePlacement, moduleDef, instantName));
        
        // Assignment for construction
        var assignName = $"[Assign] {moduleDef.moduleId} {footprintInfo}{clearanceInfo}";
        _registry.Register(new Actions.AssignModuleAction(_modulePlacement, moduleDef, assignName));
        
        count++;
      }

      if (count > 0) {
        Debug.Log($"[DebugModule] Registered {count * 2} module actions ({count} instant + {count} assign)");
      }
    }
  }
}