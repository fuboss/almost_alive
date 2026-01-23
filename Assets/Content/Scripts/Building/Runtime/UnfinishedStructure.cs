using System.Linq;
using Content.Scripts.AI.Craft;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Building.Data;
using Content.Scripts.Game;
using Content.Scripts.Game.Craft;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Building.Runtime {
  /// <summary>
  /// Blueprint structure in progress of construction.
  /// Analogous to UnfinishedActor for crafting.
  /// </summary>
  [RequireComponent(typeof(ActorInventory))]
  public class UnfinishedStructure : MonoBehaviour, IProgressProvider {
    [Title("Definition")]
    [ShowInInspector, ReadOnly]
    private StructureDefinitionSO _definition;

    [Title("Progress")]
    [ShowInInspector, ReadOnly, ProgressBar(0, 1)]
    private float _workProgress;

    [Title("Ghost")]
    [ShowInInspector, ReadOnly]
    private GameObject _ghostView;

    private ActorInventory _inventory;

    #region Properties

    public StructureDefinitionSO definition => _definition;
    public ConstructionData constructionData => _definition?.constructionData;
    public ActorInventory inventory => _inventory;
    public GameObject ghostView => _ghostView;

    public float workProgress => _workProgress;
    public float workRequired => constructionData?.workRequired ?? 0f;
    public float workRatio => workRequired > 0f ? Mathf.Clamp01(_workProgress / workRequired) : 1f;
    public bool workComplete => _workProgress >= workRequired;

    [ShowInInspector][Title("Progress")]
    public bool hasAllResources => CheckAllResourcesDelivered();
    public bool isReadyToComplete => hasAllResources && workComplete;

    // IProgressProvider
    float IProgressProvider.progress => workRatio;
    public ActorDescription actor => null;  // structures don't have ActorDescription

    #endregion

    #region Lifecycle

    private void Awake() {
      _inventory = GetComponent<ActorInventory>();
    }

    private void OnEnable() {
      Registry<UnfinishedStructure>.Register(this);
    }

    private void OnDisable() {
      Registry<UnfinishedStructure>.Unregister(this);
    }

    #endregion

    #region Initialization

    public void Initialize(StructureDefinitionSO definition) {
      _definition = definition;
      _workProgress = 0f;
      Debug.Log($"[UnfinishedStructure] Initialized for {definition.structureId}");
    }

    public void SetGhostView(GameObject ghost) {
      _ghostView = ghost;
    }

    #endregion

    #region Work Progress

    /// <summary>
    /// Add work progress. Returns true if work is complete.
    /// </summary>
    public bool AddWork(float amount) {
      if (amount <= 0f) return workComplete;
      _workProgress = Mathf.Min(_workProgress + amount, workRequired);
      return workComplete;
    }

    #endregion

    #region Resource Tracking

    /// <summary>
    /// Get remaining resource count for specific tag.
    /// </summary>
    public int GetRemainingResourceCount(string tag) {
      if (constructionData == null) return 0;
      
      var required = constructionData.requiredResources
        .Where(r => r.tag == tag)
        .Sum(r => r.count);
      var have = _inventory.GetItemCount(tag);
      return Mathf.Max(0, required - have);
    }

    /// <summary>
    /// Get all remaining resource requirements.
    /// </summary>
    public (string tag, int remaining)[] GetRemainingResources() {
      if (constructionData == null) return System.Array.Empty<(string, int)>();
      
      return constructionData.requiredResources
        .Select(r => (r.tag, GetRemainingResourceCount(r.tag)))
        .Where(x => x.Item2 > 0)
        .ToArray();
    }

    /// <summary>
    /// Check if all required resources have been delivered.
    /// </summary>
    public bool CheckAllResourcesDelivered() {
      if (constructionData == null) return false;
      
      foreach (var req in constructionData.requiredResources) {
        if (GetRemainingResourceCount(req.tag) > 0) return false;
      }
      return true;
    }

    #endregion

    #region Cleanup

    private void OnDestroy() {
      if (_ghostView != null) {
        Destroy(_ghostView);
        _ghostView = null;
      }
    }

    #endregion
  }
}
