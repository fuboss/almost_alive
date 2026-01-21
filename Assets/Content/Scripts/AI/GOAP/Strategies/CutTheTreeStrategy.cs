using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Animation;
using Content.Scripts.Game;
using Content.Scripts.Game.Trees;
using UnityEngine;
using VContainer;

namespace Content.Scripts.AI.GOAP.Strategies {
  [Serializable]
  public class CutTheTreeStrategy : AgentStrategy {
    [Inject] private TreeModule _treeModule;

    [Tooltip("Work units added per second")]
    public float workRate = 2f;

    private IGoapAgentCore _agent;
    private ITransientTargetAgent _transientAgent;
    private AnimationController _animations;
    private ChoppingProgress _choppingProgress;

    public override IActionStrategy Create(IGoapAgentCore agent) {
      return new CutTheTreeStrategy(agent) {
        workRate = workRate
      };
    }

    public CutTheTreeStrategy() {
    }

    public CutTheTreeStrategy(IGoapAgentCore agent) : this() {
      _agent = agent;
      _transientAgent = agent as ITransientTargetAgent;
      _animations = _agent.body?.animationController;
    }

    public override bool canPerform => !complete && _transientAgent?.transientTarget != null;
    public override bool complete { get; internal set; }

    public ActorDescription target { get; private set; }

    public override void OnStart() {
      complete = false;
      
      if (_transientAgent == null) {
        complete = true;
        Debug.LogWarning("[CutTree] Agent missing ITransientTargetAgent");
        return;
      }
      
      target = _transientAgent.transientTarget?.GetComponent<ActorDescription>();
      if (target == null) {
        complete = true;
        Debug.LogWarning("[CutTree] No target, abort");
        return;
      }

      var treeTag = target.GetDefinition<TreeTag>();
      if (treeTag == null) {
        complete = true;
        Debug.LogWarning("[CutTree] No TreeTag on target, abort");
        return;
      }

      _choppingProgress = ChoppingProgress.GetOrCreate(target.gameObject, treeTag.workRequired);
      _animations?.CutTree();
      Debug.Log($"[CutTree] Started chopping {target.name}, work required: {treeTag.workRequired}");
    }

    public override void OnComplete() {
      if (target == null || _transientAgent == null) return;
      _agent.memory.Forget(_transientAgent.transientTarget);
      _transientAgent.transientTarget = null;
      _treeModule.ChopDownTree(_choppingProgress, _agent);
    }

    public override void OnStop() {
      _choppingProgress = null;
    }

    public override void OnUpdate(float deltaTime) {
      if (_choppingProgress == null) return;

      var workDone = _choppingProgress.AddWork(workRate * deltaTime);
      if (workDone) {
        complete = true;
      }
    }
  }
}
