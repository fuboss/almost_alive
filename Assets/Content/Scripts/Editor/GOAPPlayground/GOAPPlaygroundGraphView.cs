using System.Collections.Generic;
using System.Linq;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Goals;
using Content.Scripts.Editor.GOAPPlayground.Nodes;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

namespace Content.Scripts.Editor.GOAPPlayground {
  public class GOAPPlaygroundGraphView : GraphView {
    private List<ActionDataSO> _sourceActions = new();
    private List<GoalSO> _sourceGoals = new();
    
    private readonly Dictionary<GoalNodeView, PlanTree> _planTrees = new();
    private readonly List<GoalNodeView> _goalNodes = new();
    private readonly List<Edge> _allEdges = new();

    public IReadOnlyList<GoalNodeView> goalNodes => _goalNodes;

    public GOAPPlaygroundGraphView() {
      SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
      
      this.AddManipulator(new ContentDragger());
      this.AddManipulator(new SelectionDragger());
      this.AddManipulator(new RectangleSelector());

      var grid = new GridBackground();
      Insert(0, grid);
      grid.StretchToParentSize();
      
      // Undo support for node movement
      graphViewChanged += OnGraphViewChanged;
    }

    private GraphViewChange OnGraphViewChanged(GraphViewChange change) {
      if (change.movedElements != null && change.movedElements.Count > 0) {
        // Undo.RegisterCompleteObjectUndo(this, "Move GOAP Nodes");
      }
      return change;
    }

    public void LoadFromResources(string[] featureNames = null) {
      // Undo.RegisterCompleteObjectUndo(this, "Load GOAP Graph");
      ClearGraph();
      
      _sourceActions = LoadAllActions(featureNames);
      _sourceGoals = LoadAllGoals(featureNames);

      foreach (var goalSO in _sourceGoals) {
        CreatePlanTree(goalSO);
      }

      AutoLayout();
    }

    private void ClearGraph() {
      graphElements.ForEach(RemoveElement);
      _goalNodes.Clear();
      _planTrees.Clear();
      _allEdges.Clear();
    }

    private List<ActionDataSO> LoadAllActions(string[] featureNames) {
      var all = Resources.LoadAll<ActionDataSO>("GOAP");
      if (featureNames == null || featureNames.Length == 0) return all.ToList();
      
      return all.Where(a => {
        var path = AssetDatabase.GetAssetPath(a);
        return featureNames.Any(f => path.Contains(f.Replace("_FeatureSet", "")));
      }).ToList();
    }

    private List<GoalSO> LoadAllGoals(string[] featureNames) {
      var all = Resources.LoadAll<GoalSO>("GOAP");
      if (featureNames == null || featureNames.Length == 0) return all.ToList();
      
      return all.Where(g => {
        var path = AssetDatabase.GetAssetPath(g);
        return featureNames.Any(f => path.Contains(f.Replace("_FeatureSet", "")));
      }).ToList();
    }

    private void CreatePlanTree(GoalSO goalSO) {
      var goalNode = new GoalNodeView(goalSO);
      AddElement(goalNode);
      _goalNodes.Add(goalNode);

      var tree = new PlanTree { goal = goalNode };
      
      var visited = new HashSet<string>();
      var actionQueue = new Queue<(string belief, ActionNodeView consumer, int depth)>();
      
      foreach (var effect in goalNode.GetDesiredEffects()) {
        actionQueue.Enqueue((effect, null, 1));
      }

      while (actionQueue.Count > 0) {
        var (belief, consumer, depth) = actionQueue.Dequeue();
        if (visited.Contains(belief) || depth > 10) continue;
        visited.Add(belief);

        var actionSO = _sourceActions.FirstOrDefault(a => 
          a.data.effects != null && a.data.effects.Contains(belief));
        
        if (actionSO == null) {
          // Create GET node for world-state belief
          var getNode = new BeliefVariableNode(belief, BeliefNodeType.Get);
          AddElement(getNode);
          tree.getNodes.Add((getNode, consumer, belief));
          continue;
        }

        var actionNode = new ActionNodeView(actionSO);
        AddElement(actionNode);
        tree.actions.Add(actionNode);
        tree.actionDepths[actionNode] = depth;

        // Create SET nodes for effects
        foreach (var eff in actionNode.GetEffects()) {
          var setNode = new BeliefVariableNode(eff, BeliefNodeType.Set);
          AddElement(setNode);
          tree.setNodes.Add((setNode, actionNode, eff));
          
          if (actionNode.beliefOutputs.TryGetValue(eff, out var effPort)) {
            CreateEdge(effPort, setNode.port, new Color(0.4f, 0.9f, 0.5f));
          }
        }

        // Execution flow
        if (consumer != null) {
          CreateEdge(actionNode.execOut, consumer.execIn, Color.white);
        } else {
          CreateEdge(actionNode.execOut, goalNode.execIn, Color.white);
        }

        // Queue preconditions
        foreach (var pre in actionNode.GetPreconditions()) {
          if (!visited.Contains(pre)) {
            actionQueue.Enqueue((pre, actionNode, depth + 1));
          }
        }
      }

      // Connect GET nodes to action preconditions
      foreach (var (getNode, consumer, belief) in tree.getNodes) {
        if (consumer != null && consumer.beliefInputs.TryGetValue(belief, out var prePort)) {
          CreateEdge(getNode.port, prePort, new Color(1f, 0.7f, 0.4f));
        } else if (goalNode.beliefInputs.TryGetValue(belief, out var goalPort)) {
          CreateEdge(getNode.port, goalPort, new Color(1f, 0.7f, 0.4f));
        }
      }

      _planTrees[goalNode] = tree;
    }

    private void CreateEdge(Port output, Port input, Color color) {
      if (output == null || input == null) return;
      var edge = new Edge { output = output, input = input };
      edge.output.Connect(edge);
      edge.input.Connect(edge);
      edge.edgeControl.inputColor = color;
      edge.edgeControl.outputColor = color;
      AddElement(edge);
      _allEdges.Add(edge);
    }

    private void AutoLayout() {
      const float actionWidth = 180f;
      const float actionHeight = 90f;
      const float beliefWidth = 100f;
      const float beliefHeight = 28f;
      const float hGap = 60f;
      const float vGap = 50f;
      const float beliefGap = 8f;
      const float treeGap = 150f;

      var currentY = 50f;

      foreach (var goal in _goalNodes) {
        var tree = _planTrees.GetValueOrDefault(goal);
        if (tree == null) continue;

        // Group actions by depth
        var byDepth = new Dictionary<int, List<ActionNodeView>>();
        foreach (var action in tree.actions) {
          var d = tree.actionDepths.GetValueOrDefault(action, 1);
          if (!byDepth.ContainsKey(d)) byDepth[d] = new List<ActionNodeView>();
          byDepth[d].Add(action);
        }

        var maxDepth = byDepth.Count > 0 ? byDepth.Keys.Max() : 0;
        var maxInColumn = byDepth.Count > 0 ? byDepth.Values.Max(v => v.Count) : 1;
        
        // Calculate tree height based on actions + belief nodes
        var actionAreaHeight = maxInColumn * (actionHeight + vGap);
        var treeHeight = actionAreaHeight;

        // Goal position (rightmost)
        var goalX = 100 + (maxDepth + 1) * (actionWidth + hGap + beliefWidth * 2 + beliefGap * 2);
        goal.SetPosition(new Rect(goalX, currentY + treeHeight / 2 - actionHeight / 2, actionWidth, actionHeight));

        // Position actions and their belief nodes
        foreach (var kvp in byDepth) {
          var depth = kvp.Key;
          var actions = kvp.Value;
          
          var actionX = goalX - depth * (actionWidth + hGap + beliefWidth * 2 + beliefGap * 2);
          var colHeight = actions.Count * (actionHeight + vGap) - vGap;
          var startY = currentY + (treeHeight - colHeight) / 2;

          for (int i = 0; i < actions.Count; i++) {
            var action = actions[i];
            var actionY = startY + i * (actionHeight + vGap);
            action.SetPosition(new Rect(actionX, actionY, actionWidth, actionHeight));

            // GET nodes (preconditions) - left of action
            var getNodesForAction = tree.getNodes.Where(g => g.consumer == action).ToList();
            var getStartY = actionY + (actionHeight - getNodesForAction.Count * (beliefHeight + beliefGap)) / 2;
            for (int j = 0; j < getNodesForAction.Count; j++) {
              var getY = getStartY + j * (beliefHeight + beliefGap);
              getNodesForAction[j].node.SetPosition(new Rect(
                actionX - beliefWidth - beliefGap, getY, beliefWidth, beliefHeight));
            }

            // SET nodes (effects) - right of action
            var setNodesForAction = tree.setNodes.Where(s => s.action == action).ToList();
            var setStartY = actionY + (actionHeight - setNodesForAction.Count * (beliefHeight + beliefGap)) / 2;
            for (int j = 0; j < setNodesForAction.Count; j++) {
              var setY = setStartY + j * (beliefHeight + beliefGap);
              setNodesForAction[j].node.SetPosition(new Rect(
                actionX + actionWidth + beliefGap, setY, beliefWidth, beliefHeight));
            }
          }
        }

        // GET nodes connected to goal (no consumer action)
        var goalGetNodes = tree.getNodes.Where(g => g.consumer == null).ToList();
        var goalGetStartY = currentY + treeHeight / 2 - goalGetNodes.Count * (beliefHeight + beliefGap) / 2;
        for (int j = 0; j < goalGetNodes.Count; j++) {
          var getY = goalGetStartY + j * (beliefHeight + beliefGap);
          goalGetNodes[j].node.SetPosition(new Rect(
            goalX - beliefWidth - beliefGap, getY, beliefWidth, beliefHeight));
        }

        currentY += treeHeight + treeGap;
      }
    }

    public void HighlightPlan(string goalName, List<string> actionNames) {
      // Reset
      foreach (var goal in _goalNodes) {
        goal.SetHighlight(false);
        goal.SetExecutionHighlight(false);
      }
      foreach (var tree in _planTrees.Values) {
        foreach (var action in tree.actions) {
          action.SetHighlight(false);
          action.SetExecutionHighlight(false);
        }
        foreach (var (node, _, _) in tree.getNodes) node.SetHighlight(false);
        foreach (var (node, _, _) in tree.setNodes) node.SetHighlight(false);
      }

      var targetGoal = _goalNodes.FirstOrDefault(g => g.goalData.name == goalName);
      if (targetGoal == null || !_planTrees.TryGetValue(targetGoal, out var planTree)) return;

      var planColor = new Color(0.2f, 1f, 0.3f);
      targetGoal.SetHighlight(true, planColor);

      foreach (var actionName in actionNames) {
        var action = planTree.actions.FirstOrDefault(a => a.actionData.name == actionName);
        if (action != null) {
          action.SetHighlight(true, planColor);
          action.SetExecutionHighlight(true);
        }
      }
    }

    public void HighlightPlan(List<string> actionNames) {
      foreach (var tree in _planTrees.Values) {
        foreach (var action in tree.actions) {
          action.SetHighlight(actionNames.Contains(action.actionData.name), new Color(0.2f, 0.9f, 0.3f));
        }
      }
    }

    public HashSet<string> GetAllBeliefs() {
      var beliefs = new HashSet<string>();
      foreach (var action in _sourceActions) {
        if (action.data.preconditions != null)
          foreach (var p in action.data.preconditions) beliefs.Add(p);
        if (action.data.effects != null)
          foreach (var e in action.data.effects) beliefs.Add(e);
      }
      foreach (var goal in _sourceGoals) {
        if (goal.template.desiredEffects != null)
          foreach (var e in goal.template.desiredEffects) beliefs.Add(e);
      }
      return beliefs;
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter) {
      return ports.ToList().Where(p => 
        p.direction != startPort.direction && 
        p.node != startPort.node &&
        p.portType == startPort.portType
      ).ToList();
    }

    private class PlanTree {
      public GoalNodeView goal;
      public List<ActionNodeView> actions = new();
      public List<(BeliefVariableNode node, ActionNodeView consumer, string belief)> getNodes = new();
      public List<(BeliefVariableNode node, ActionNodeView action, string belief)> setNodes = new();
      public Dictionary<ActionNodeView, int> actionDepths = new();
    }
  }
}
