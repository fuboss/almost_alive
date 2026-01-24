using System.Collections.Generic;
using Content.Scripts.AI.Craft;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Game.Craft {
  public interface IProgressProvider {
    float progress { get; }
    ActorDescription actor { get; }
  }

  public class UnfinishedActor : UnfinishedActorBase {
    [ShowInInspector, ReadOnly] private RecipeSO _recipe;

    public RecipeSO recipe => _recipe;
    public override float workRequired => _recipe?.recipe.workRequired ?? 0f;
    protected override IReadOnlyList<RecipeRequiredResource> requiredResources 
      => _recipe?.recipe.requiredResources;

    public void Initialize(RecipeSO recipe) {
      _recipe = recipe;
      _workProgress = 0f;
    }

    public override ActorDescription TryComplete() {
      if (!isReadyToComplete) {
        Debug.LogWarning($"[Unfinished] Cannot complete - resources: {hasAllResources}, work: {workComplete}");
        return null;
      }

      if (_actorCreation == null) {
        Debug.LogError("[Unfinished] ActorCreationModule not injected!");
        return null;
      }

      var pos = transform.position;

      if (!_actorCreation.TrySpawnActorOnGround(_recipe.recipe.resultActorKey, pos, out var result,
            _recipe.recipe.outputCount)) {
        Debug.LogError($"[Unfinished] Failed to spawn {_recipe.recipe.resultActorKey}");
        return null;
      }

      Debug.Log($"[Unfinished] Completed! Spawned {result.actorKey}");
      Destroy(gameObject);

      return result;
    }
  }
}