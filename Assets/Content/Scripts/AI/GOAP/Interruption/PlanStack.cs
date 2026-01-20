using System.Collections.Generic;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent.Memory;
using Content.Scripts.AI.GOAP.Goals;
using Content.Scripts.AI.GOAP.Planning;

namespace Content.Scripts.AI.GOAP.Interruption {
  /// <summary>
  /// Stack of saved plans for interrupt/resume functionality.
  /// </summary>
  public class PlanStack {
    private readonly Stack<SavedPlan> _stack = new();
    private readonly int _maxDepth;

    public struct SavedPlan {
      public AgentGoal goal;
      public ActionPlan plan;
      public MemorySnapshot targetSnapshot;
      
      public bool isValid => goal != null && plan != null;
    }

    public int count => _stack.Count;
    public bool isEmpty => _stack.Count == 0;
    public bool canPush => _stack.Count < _maxDepth;

    public PlanStack(int maxDepth = 3) {
      _maxDepth = maxDepth;
    }

    /// <summary>
    /// Save current plan to stack. Returns false if stack is full.
    /// </summary>
    public bool TryPush(AgentGoal goal, ActionPlan plan, MemorySnapshot target = null) {
      if (!canPush) return false;
      if (goal == null || plan == null) return false;

      _stack.Push(new SavedPlan {
        goal = goal,
        plan = plan,
        targetSnapshot = target
      });

      return true;
    }

    /// <summary>
    /// Restore most recent saved plan.
    /// </summary>
    public bool TryPop(out SavedPlan saved) {
      if (_stack.Count == 0) {
        saved = default;
        return false;
      }

      saved = _stack.Pop();
      return saved.isValid;
    }

    /// <summary>
    /// Peek at top plan without removing.
    /// </summary>
    public bool TryPeek(out SavedPlan saved) {
      if (_stack.Count == 0) {
        saved = default;
        return false;
      }

      saved = _stack.Peek();
      return true;
    }

    public void Clear() {
      _stack.Clear();
    }
  }
}
