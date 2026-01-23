using System;
using System.Collections.Generic;
using Content.Scripts.AI.Craft;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Building.Data {
  /// <summary>
  /// Construction requirements for structures.
  /// Unlike RecipeData, does not specify result — structure knows itself.
  /// </summary>
  [Serializable]
  public class ConstructionData : IConstructionRequirements {
    [Tooltip("Required resources: tag -> count")]
    [SerializeField]
    private List<RecipeRequiredResource> _requiredResources = new();

    [Tooltip("Work units required")]
    [MinValue(0f)]
    public float workRequired = 10f;

    public IReadOnlyList<RecipeRequiredResource> requiredResources => _requiredResources;

    IReadOnlyList<RecipeRequiredResource> IConstructionRequirements.requiredResources => _requiredResources;
    float IConstructionRequirements.workRequired => workRequired;
  }
}
