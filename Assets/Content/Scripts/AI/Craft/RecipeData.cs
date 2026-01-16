using System;
using System.Collections.Generic;
using Content.Scripts.AI.GOAP;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.AI.Craft {
  [Serializable]
  public class RecipeData {
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

    public List<RecipeRequiredResource> requiredResources => _requiredResources;

#if UNITY_EDITOR
    private IEnumerable<string> ActorKeys() => GOAPEditorHelper.GetActorKeys();
#endif
  }
}