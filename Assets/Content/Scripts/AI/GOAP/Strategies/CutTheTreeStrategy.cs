using System;
using Content.Scripts.AI.GOAP.Actions;
using Content.Scripts.AI.GOAP.Agent;
using Content.Scripts.Animation;
using Content.Scripts.Game;
using Content.Scripts.Game.Interaction;
using ImprovedTimers;
using UnityEngine;
using VContainer;

namespace Content.Scripts.AI.GOAP.Strategies {
  [Serializable]
  public class CutTheTreeStrategy : AgentStrategy {
    [Inject] private ActorDestructionModule _destructionModule;
    public float duration = 3f;
    private readonly AnimationController _animations;
    private readonly IGoapAgent _agent;
    private CountdownTimer _timer;

    public override IActionStrategy Create(IGoapAgent agent) {
      return new CutTheTreeStrategy(agent) {
        duration = duration
      };
    }

    public CutTheTreeStrategy() {
    }

    public CutTheTreeStrategy(IGoapAgent agent) : this() {
      _agent = agent;
      _animations = _agent.animationController;
    }

    public override bool canPerform => !complete && _agent?.transientTarget != null;
    public override bool complete { get; internal set; }

    public ActorDescription target { get; private set; }

    public override void OnStart() {
      target = _agent?.transientTarget != null
        ? _agent.transientTarget.GetComponent<ActorDescription>()
        : null;
      if (target == null) {
        complete = true;
        Debug.LogError("abort");
        return;
      }

      IniTimer();
      _timer.Start();
      _animations.CutTree();
    }

    private void IniTimer() {
      _timer?.Dispose();
      _timer = new CountdownTimer(duration); //animations.GetAnimationLength(animations.)

      _timer.OnTimerStart += () => complete = false;
      _timer.OnTimerStop += () => complete = true;
    }

    public override void OnComplete() {
      if (target == null) return;
      if (_agent.inventory.TryPutItemInInventory(target)) {
        Debug.Log($"{target.name} cut complete!");
        _agent.memory.Forget(_agent.transientTarget);
        _agent.transientTarget = null;
        _destructionModule?.DestroyActor(target);
      }
      else {
        Debug.LogError($"failed to cut  {target.name}", target);
      }
    }

    public override void OnStop() {
      _timer?.Dispose();
    }

    public override void OnUpdate(float deltaTime) {
      _timer.Tick();
    }
  }
}