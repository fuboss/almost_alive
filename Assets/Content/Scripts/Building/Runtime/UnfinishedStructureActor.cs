using System.Collections.Generic;
using Content.Scripts.AI.Craft;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Building.Data;
using Content.Scripts.Game;
using Content.Scripts.Game.Craft;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Building.Runtime {
  public class UnfinishedStructureActor : UnfinishedActorBase {
    [Title("Definition")] [ShowInInspector, ReadOnly]
    private StructureDefinitionSO _definition;

    [Title("Ghost")] [ShowInInspector]
    private GameObject _ghostView;

    public StructureDefinitionSO definition => _definition;
    public ConstructionData constructionData => _definition?.constructionData;
    public GameObject ghostView => _ghostView;

    public override float workRequired => constructionData?.workRequired ?? 100;
    protected override IReadOnlyList<RecipeRequiredResource> requiredResources 
      => constructionData?.requiredResources;

    public void Initialize(StructureDefinitionSO definition) {
      _definition = definition;
      _workProgress = 0f;
    }

    public void SetGhostView(GameObject ghost) {
      _ghostView = ghost;
    }

    public override ActorDescription TryComplete() {
      if (!isReadyToComplete) {
        Debug.LogWarning($"[UnfinishedStructure] Cannot complete - resources: {hasAllResources}, work: {workComplete}");
        return null;
      }

      if (_actorCreation == null) {
        Debug.LogError("[UnfinishedStructure] ActorCreationModule not injected!");
        return null;
      }

      if (!_actorCreation.TrySpawnActor(definition.structureId, transform.position, out var result)) {
        Debug.LogError($"[UnfinishedStructure] Failed to spawn {definition.structureId}");
        return null;
      }

      var structure = result.GetComponent<Structure>();
      if (structure == null) return null;
      
      structure.Initialize(_definition);
      Destroy(gameObject);


      return result;
    }

    private void OnDestroy() {
      if (_ghostView == null) return;
      Destroy(_ghostView);
      _ghostView = null;
    }
  }
}