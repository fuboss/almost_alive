using System;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Game.Work {
  /// <summary>
  /// Agent's work priorities. Determines what work agent will do.
  /// Priority 0 = disabled, 1 = lowest, 4 = highest.
  /// </summary>
  [Serializable]
  public class WorkPriority : SerializedMonoBehaviour {
    [DictionaryDrawerSettings(KeyLabel = "Work", ValueLabel = "Priority")] [SerializeField]
    private Dictionary<WorkType, int> _priorities = new();

    public const int DISABLED = 0;
    public const int MIN_PRIORITY = 1;
    public const int MAX_PRIORITY = 4;

    private void Awake() {
      InitializeDefaults();
    }

    [Button]
    private void InitializeDefaults() {
      foreach (WorkType workType in Enum.GetValues(typeof(WorkType))) {
        if (workType == WorkType.NONE) continue;
        _priorities.TryAdd(workType, 3); // default middle priority
      }
    }

    public int GetPriority(WorkType workType) {
      return _priorities.GetValueOrDefault(workType, DISABLED);
    }

    public void SetPriority(WorkType workType, int priority) {
      _priorities[workType] = Mathf.Clamp(priority, DISABLED, MAX_PRIORITY);
    }

    public bool CanDoWork(WorkType workType) {
      return GetPriority(workType) > DISABLED;
    }

    public bool IsDisabled(WorkType workType) {
      return GetPriority(workType) == DISABLED;
    }

    /// <summary>
    /// Get highest priority work type that agent can do.
    /// </summary>
    public WorkType GetHighestPriorityWork() {
      var highest = WorkType.NONE;
      var highestPriority = DISABLED;

      foreach (var kvp in _priorities) {
        if (kvp.Value > highestPriority) {
          highestPriority = kvp.Value;
          highest = kvp.Key;
        }
      }

      return highest;
    }

    /// <summary>
    /// Get all work types sorted by priority (highest first).
    /// </summary>
    public IEnumerable<WorkType> GetWorksByPriority() {
      return _priorities
        .Where(kvp => kvp.Value > DISABLED)
        .OrderByDescending(kvp => kvp.Value)
        .Select(kvp => kvp.Key);
    }

    /// <summary>
    /// Get all enabled work types.
    /// </summary>
    public IEnumerable<WorkType> GetEnabledWorks() {
      return _priorities
        .Where(kvp => kvp.Value > DISABLED)
        .Select(kvp => kvp.Key);
    }

    /// <summary>
    /// Disable all work types.
    /// </summary>
    public void DisableAll() {
      foreach (var key in _priorities.Keys.ToList()) {
        _priorities[key] = DISABLED;
      }
    }

    /// <summary>
    /// Enable only specified work type.
    /// </summary>
    public void SetOnlyWork(WorkType workType, int priority = MAX_PRIORITY) {
      DisableAll();
      SetPriority(workType, priority);
    }

    public IReadOnlyDictionary<WorkType, int> GetAllPriorities() => _priorities;
  }
}