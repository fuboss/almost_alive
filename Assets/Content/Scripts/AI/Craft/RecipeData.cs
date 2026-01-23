using System;
using System.Collections.Generic;
using Content.Scripts.AI.GOAP;
using Content.Scripts.Building.Data;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.Craft {
  [Serializable]
  public class RecipeData : IConstructionRequirements {
    [Tooltip("ActorKey of the result prefab")] [ValueDropdown("ActorKeys")]
    public string resultActorKey;

    [Tooltip("Required resources: tag -> count")] [SerializeField]
    private List<RecipeRequiredResource> _requiredResources = new();

    [Tooltip("Time in seconds to craft")] [MinValue(0.1f)]
    public float craftTime = 5f;

    [Tooltip("What station is needed (None = by hand)")]
    public CraftStationType stationType = CraftStationType.None;

    [Tooltip("How many items produced per craft")] [MinValue(1)]
    public ushort outputCount = 1;

    [Tooltip("Work units required (0 = instant)")] [MinValue(0f)]
    public float workRequired = 0f;

    public List<RecipeRequiredResource> requiredResources => _requiredResources;

    // IConstructionRequirements implementation
    IReadOnlyList<RecipeRequiredResource> IConstructionRequirements.requiredResources => _requiredResources;
    float IConstructionRequirements.workRequired => workRequired;

#if UNITY_EDITOR
    private IEnumerable<string> ActorKeys() => GOAPEditorHelper.GetActorKeys();
#endif
  }
}
