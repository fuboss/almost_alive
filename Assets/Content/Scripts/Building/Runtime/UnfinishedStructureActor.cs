using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.Craft;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Building.Data;
using Content.Scripts.Building.Services;
using Content.Scripts.Game;
using Content.Scripts.Game.Craft;
using Sirenix.OdinInspector;
using UnityEngine;
using VContainer;

namespace Content.Scripts.Building.Runtime {
  /// <summary>
  /// Blueprint structure in progress of construction.
  /// Analogous to UnfinishedActor for crafting.
  /// </summary>
  [RequireComponent(typeof(ActorDescription))]
  [RequireComponent(typeof(ActorInventory))]
  public class UnfinishedStructureActor : UnfinishedActor {
    [Inject] private StructuresModule _structuresModule;
    
    [Title("Definition")] [ShowInInspector, ReadOnly]
    private StructureDefinitionSO _definition;

    [Title("Ghost")] [ShowInInspector]
    private GameObject _ghostView;

    public StructureDefinitionSO definition => _definition;
    public ConstructionData constructionData => _definition?.constructionData;
    public GameObject ghostView => _ghostView;

    public void Initialize(StructureDefinitionSO definition) {
      _definition = definition;
      _workProgress = 0f;
      Debug.Log($"[UnfinishedStructure] Initialized for {definition.structureId}");
    }

    public void SetGhostView(GameObject ghost) {
      _ghostView = ghost;
    }

    public override ActorDescription TryComplete() {
      Debug.LogError($"[UnfinishedStructure] Trying to Complete structure {definition.structureId}", this);
      if (!isReadyToComplete) {
        Debug.LogWarning($"[UnfinishedStructure] Cannot complete - resources: {hasAllResources}, work: {workComplete}");
        return null;
      }

      if (_actorCreation == null) {
        Debug.LogError("[UnfinishedStructure] ActorCreationModule not injected!");
        return null;
      }

      
      if (!_actorCreation.TrySpawnActor(_recipe.recipe.resultActorKey, transform.position, out var result)) {
        Debug.LogError($"[UnfinishedStructure] Failed to spawn {_recipe.recipe.resultActorKey}");
        return null;
      }
      var structure = actor.GetComponent<Structure>();
      if (structure == null) {
        return null;
      }
      structure.Initialize(_definition);
      Debug.Log($"[UnfinishedStructure] Completed! Spawned {result.actorKey}");
      Destroy(gameObject);
      
      return result;
    }


    protected override IReadOnlyList<RecipeRequiredResource> requiredResources
      => constructionData.requiredResources;

    /// <summary>
    /// Get remaining resource count for specific tag.
    /// </summary>
    public override int GetRemainingResourceCount(string tag) {
      return constructionData == null ? 0 : base.GetRemainingResourceCount(tag);
    }

    /// <summary>
    /// Get all remaining resource requirements.
    /// </summary>
    public override (string tag, int remaining)[] GetRemainingResources() {
      return constructionData == null ? System.Array.Empty<(string, int)>() : base.GetRemainingResources();
    }

    /// <summary>
    /// Check if all required resources have been delivered.
    /// </summary>
    public override bool CheckAllResourcesDelivered() {
      return constructionData != null && base.CheckAllResourcesDelivered();
    }

    private void OnDestroy() {
      if (_ghostView == null) return;
      Destroy(_ghostView);
      _ghostView = null;
    }
  }
}