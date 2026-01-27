using Sirenix.OdinInspector;
using UnityEngine;

namespace Content.Scripts.Game.Work {
  /// <summary>
  /// Marks an actor for specific work type. Used by colonists to find work.
  /// </summary>
  public class WorkMarker : MonoBehaviour {
    [ShowInInspector, ReadOnly] private WorkType _workType;
    [ShowInInspector, ReadOnly] private bool _isMarked;
    [ShowInInspector, ReadOnly] private int _priority = 1;

    public WorkType workType => _workType;
    public bool isMarked => _isMarked;
    public int priority => _priority;

    public void Mark(WorkType type, int priority = 1) {
      _workType = type;
      _priority = priority;
      _isMarked = true;
      
      // TODO: Register with WorkAssignmentModule
      // TODO: Visual indicator
    }

    public void Unmark() {
      _isMarked = false;
      _workType = WorkType.NONE;
      
      // TODO: Unregister from WorkAssignmentModule
    }
  }
}
